Imports System
Imports System.Net
Imports System.IO
Imports System.Drawing.Printing
Imports Newtonsoft.Json.Linq
Imports Newtonsoft.Json
Imports System.Text
Imports System.Net.Sockets
Imports System.ComponentModel
Imports System.IO.Ports
Imports PdfiumViewer

Public Class frmMain

    Class config
        Property listening_port As Integer = -1
        Property printer1 As String = ""
        Property printer2 As String = ""
        Property printer3 As String = ""

        Private Shared fl As String = ".\data\appconfig.js"
        Shared Function load() As config
            If Directory.Exists(".\data") = False Then Directory.CreateDirectory(".\data")
            If File.Exists(fl) Then
                Return JsonConvert.DeserializeObject(Of config)(File.ReadAllText(fl, New UTF8Encoding(False)))
            End If
            Return New config().Save()
        End Function

        Function Save() As config
            Dim js = JsonConvert.SerializeObject(Me, Formatting.Indented)
            File.WriteAllText(fl, js, New UTF8Encoding(False))
            Return Me
        End Function
    End Class


    Private WithEvents _BG As BackgroundWorker = Nothing

    Private server As HttpListener = Nothing

    Private APP_CONFIG As config = Nothing


#Region "Helpers"

    Sub HideToTray()
        NotifyIcon1.Visible = Not NotifyIcon1.Visible
        If NotifyIcon1.Visible = False Then
            Me.Show()
        Else
            NotifyIcon1.Text = Me.Text
            NotifyIcon1.ShowBalloonTip(2500, "Print Server", "Print Server is minimized to tray...", ToolTipIcon.Info)
            If IsNothing(_BG) OrElse _BG.IsBusy = False Then
                NotifyIcon1.Text = $"{Me.Text} - Idle"
            Else
                NotifyIcon1.Text = $"{Me.Text} - Running"
            End If
            Me.Hide()
        End If
    End Sub

    Sub UpdateLogText(text As String, Optional WritetologFile As Boolean = True)
        Try
            If Me.InvokeRequired Then
                Me.Invoke(New MethodInvoker(Sub() UpdateLogText(text, WritetologFile)))
            Else
                Dim log As String = String.Format("({0}) - {1}", {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), text}) & vbCrLf
                Dim c As Color = Color.DarkSlateGray
                If System.Text.RegularExpressions.Regex.Match(log, "error:?|exception", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Success Then
                    c = Color.DarkRed
                ElseIf System.Text.RegularExpressions.Regex.Match(log, "info:?", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Success Then
                    c = Color.DarkSlateBlue
                End If
                'txtLogs.AppendText(log)
                AppendColoredText(c, log)
                If WritetologFile Then WriteToLog(text)
            End If
        Catch ex As Exception
        End Try
    End Sub

    ' Append text of the given color.
    Private AppendColoredTextObj As New Object
    Private Sub AppendColoredText(c As Color, text As String)
        Dim nStart As Integer = txtLogs.TextLength
        txtLogs.AppendText(text)
        Dim nEnd As Integer = txtLogs.TextLength

        ' Textbox may transform chars, so (end-start) != text.Length
        txtLogs.Select(nStart, nEnd - nStart)
        txtLogs.SelectionColor = c
        txtLogs.SelectionLength = 0 ' clear
    End Sub

    ''' <summary>
    ''' Get free port to use.
    ''' </summary>
    ''' <param name="count"></param>
    ''' <returns></returns>
    Public Function GetFreePort(count) As Integer()
        ' Create an array to store the free ports
        Dim ports As New List(Of Integer)

        ' Try to find a free port the specified number of times
        For i As Integer = 1 To count
            ' Create a new TcpListener on a random port
            Dim listener As New Sockets.TcpListener(IPAddress.Any, 0)

            ' Start the listener
            listener.Start()

            ' Get the port that the listener is using
            Dim port As Integer = CType(listener.LocalEndpoint, IPEndPoint).Port

            ' Add the port to the list of free ports
            ports.Add(port)

            ' Stop the listener
            listener.Stop()
        Next

        ' Return the list of free ports as an array
        Return ports.ToArray()
    End Function

    Public Shared Function GetLocalIPAddress() As String
        Try
            Return Dns.GetHostAddresses(Dns.GetHostName).Where(Function(x) x.AddressFamily = Sockets.AddressFamily.InterNetwork).LastOrDefault.ToString()
            'Return "127.0.0.1" ' Return loopback if no other IP is found
        Catch ex As Exception
            Return "Error: " & ex.Message
        End Try
    End Function

#End Region


    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        APP_CONFIG = config.load()

        If APP_CONFIG.listening_port <= 0 Then
            Dim ports = GetFreePort(5)(0) ' get first free port
            APP_CONFIG.listening_port = ports
            APP_CONFIG.Save()
        End If

        Me.CenterToScreen()

        loadPrinters()

        Me.Text = $"PRINT SERVER"
        Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)

        NotifyIcon1.Visible = False
        NotifyIcon1.Icon = Me.Icon

    End Sub

    Private Sub frmMain_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If IsNothing(_BG) Then Return
        If _BG.IsBusy Then
            Beep()
            e.Cancel = True
            Return
        End If
    End Sub

    Sub loadPrinters()
        cboPrinter1.Items.Clear()
        cboPrinter2.Items.Clear()
        cboPrinter3.Items.Clear()

        Dim l1 = PrinterSettings.InstalledPrinters
        For i = 0 To l1.Count - 1
            cboPrinter1.Items.Add(l1(i))
            cboPrinter2.Items.Add(l1(i))
            cboPrinter3.Items.Add(l1(i))
        Next

        ' set values
        cboPrinter1.Text = APP_CONFIG.printer1
        cboPrinter2.Text = APP_CONFIG.printer2
        cboPrinter3.Text = APP_CONFIG.printer3
        txtPort.Text = APP_CONFIG.listening_port

    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        ' minimize to tray
        HideToTray()
    End Sub

    Private Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        ' start listener
        StartServer()
    End Sub

    Private Sub btnQuit_Click(sender As Object, e As EventArgs) Handles btnQuit.Click
        ' check if running, ask to quit
        Me.Close()
    End Sub

    Sub StartServer()

        If _BG Is Nothing Then
            _BG = New BackgroundWorker
            _BG.WorkerReportsProgress = True
            _BG.WorkerSupportsCancellation = True
        End If

        If _BG.IsBusy = False Then
            If String.IsNullOrWhiteSpace(cboPrinter1.Text) AndAlso String.IsNullOrWhiteSpace(cboPrinter2.Text) AndAlso String.IsNullOrWhiteSpace(cboPrinter3.Text) Then
                MsgBox("Please select at least 1 printer before starting", vbExclamation)
                Return
            End If

            LockUnlock()

            APP_CONFIG.printer1 = cboPrinter1.Text
            APP_CONFIG.printer2 = cboPrinter2.Text
            APP_CONFIG.printer3 = cboPrinter3.Text
            APP_CONFIG.listening_port = txtPort.Text
            APP_CONFIG.Save()

            _BG.RunWorkerAsync()

            btnStart.Text = "&Stop"

        Else
            If _BG.CancellationPending = False Then
                btnStart.Text = "Wait"
                btnStart.Enabled = False
                StopServer()
                _BG.CancelAsync()
            End If
        End If

    End Sub
    Public Sub StopServer()
        If server IsNot Nothing AndAlso server.IsListening Then
            server.Stop()
            server.Close()
        End If
    End Sub

    Sub LockUnlock()

        cboPrinter1.Enabled = Not cboPrinter1.Enabled
        cboPrinter2.Enabled = Not cboPrinter2.Enabled
        cboPrinter3.Enabled = Not cboPrinter3.Enabled
        txtPort.Enabled = Not txtPort.Enabled
        chkAutoStart.Enabled = Not chkAutoStart.Enabled

        If txtPort.Enabled Then
            btnStart.Text = "&Start"
            btnStart.Enabled = True
        End If

    End Sub

    Dim WithEvents _printDoc As New PrintDocument()

    Sub Print(printer As String, filename As String)
        ' Determine the file type based on the file extension
        Dim fileExtension As String = System.IO.Path.GetExtension(filename).ToLower()

        Select Case fileExtension
            Case ".pdf"
                ' Print a PDF file
                ' required nugget PdfiumViewer
                ' required nugget PdfiumViewer.Native.x86_64.v8-xfa
                Using pdf = PdfDocument.Load(filename)
                    Using printdoc = pdf.CreatePrintDocument()
                        'AddHandler printdoc.EndPrint, AddressOf _printDoc_EndPrint

                        printdoc.PrinterSettings.PrinterName = APP_CONFIG.printer1
                        printdoc.Print()
                    End Using
                End Using

            'Case ".doc", ".docx"
            '    ' Print a Word document
            '    Dim wordApp As New Word.Application()
            '    Dim wordDoc As Word.Document = wordApp.Documents.Open(filePath)
            '    wordDoc.PrintOut(PrintRange:=Word.WdPrintOutRange.wdPrintAllDocument, OutputFileName:=Nothing, From:=1, To:=1, Item:=Word.WdPrintOutItem.wdPrintDocumentContent, Copies:=1, Pages:="", PageType:=Word.WdPrintOutPages.wdPrintAllPages, PrintToFile:=False, Collate:=True)
            '    wordDoc.Close()
            '    wordApp.Quit()
            'Case ".xls", ".xlsx"
            '    ' Print an Excel spreadsheet
            '    Dim excelApp As New Excel.Application()
            '    Dim excelWB As Excel.Workbook = excelApp.Workbooks.Open(filePath)
            '    excelWB.PrintOut(From:=1, To:=1, Copies:=1, Preview:=False, ActivePrinter:=printDoc.PrinterSettings.PrinterName)
            '    excelWB.Close()
            '    excelApp.Quit()

            Case ".txt"
                ' Print a plain text file
                'Dim textLines() As String = System.IO.File.ReadAllLines(filePath)
                'For Each line As String In textLines
                '    e.Graphics.DrawString(line, New Font("Arial", 12), Brushes.Black, 0, e.MarginBounds.Top)
                '    e.MarginBounds.Y += 20
                'Next

            Case Else
                ' Unsupported file type
                ConsoleWriter.WriteToLog("Unsupported file type: " & fileExtension)
        End Select
    End Sub

    Private Sub _printDoc_PrintPage(sender As Object, e As PrintPageEventArgs) Handles _printDoc.PrintPage

    End Sub

    Private Sub _printDoc_BeginPrint(sender As Object, e As PrintEventArgs) Handles _printDoc.BeginPrint

    End Sub

    Private Sub _printDoc_EndPrint(sender As Object, e As PrintEventArgs) Handles _printDoc.EndPrint

    End Sub

    Private Sub _BG_DoWork(sender As Object, e As DoWorkEventArgs) Handles _BG.DoWork

        If Not HttpListener.IsSupported Then
            Throw New Exception($"HTTP Listener not supported on this machine!")
        End If

        If server IsNot Nothing Then
            If server.IsListening Then
                server.Stop()
                server = Nothing
            End If
        End If

        UpdateLogText("Starting ...")
        UpdateLogText($"Listening To: http://localhost:{APP_CONFIG.listening_port}/")
        UpdateLogText("Waiting to receive print request")

        ' listen to this url prefix
        server = New HttpListener
        server.Prefixes.Add($"http://localhost:{APP_CONFIG.listening_port}/")
        server.Start()

        If Debugger.IsAttached Then
            Process.Start("http://localhost:49956/")
        End If

        While True
            Try
                'TODO: dont forget to verify request
                If _BG.CancellationPending Then
                    e.Cancel = True
                    Exit While
                End If

                ' Wait for a request
                Dim context As HttpListenerContext = server.GetContext()

                ' Get the request object
                Dim request As HttpListenerRequest = context.Request

                ' Get the response object
                Dim response As HttpListenerResponse = context.Response

                UpdateLogText($"Server received request: {request.Url}")

                ' receive local file path from query;
                'ex http://localhost:49956/?filename=C:\Users\TAR1PROG1\Downloads\New%20folder\NCAAF%20-%20Sportsbettingstats.com_Scraper___Mark_Corenelius__08-11-2024.xlsx
                'ex http://localhost:49956/?filename=C:\Users\TAR1PROG1\source\repos\PrintServer\bin\test.pdf
                Dim fl = request.QueryString.Get("filename")
                If String.IsNullOrWhiteSpace(fl) = False Then
                    ' check if file exists
                    If File.Exists(fl) = False Then
                        SendResponse(response, "not found",, 404)
                    Else
                        ' TODO:
                        ' verify if printer is found/active/with error
                        ' send to selected printer
                        '
                        Dim pr = request.QueryString.Get("printer")
                        If String.IsNullOrWhiteSpace(pr) Then ' defaults to printer 1
                            pr = "1"
                        End If

                        If pr = "1" Then
                            Print(APP_CONFIG.printer1, fl)

                        ElseIf pr = "2" Then
                            Print(APP_CONFIG.printer2, fl)

                        ElseIf pr = "3" Then
                            Print(APP_CONFIG.printer3, fl)

                        End If

                        SendResponse(response, "ok")

                    End If
                Else
                    SendResponse(response, "ready")
                End If

            Catch ex As Exception
                Console.WriteLine($"Error: {ex.Message}")
            End Try
        End While

    End Sub

    Sub SendResponse(response As HttpListenerResponse, text As String, Optional contenttype As String = "text/plain",
                     Optional statuscode As Integer = 200)

        ' Prepare response
        Dim responseString As String = text
        Dim buffer As Byte() = Encoding.UTF8.GetBytes(responseString)

        ' Set response headers
        response.ContentLength64 = buffer.Length
        response.ContentType = contenttype
        response.StatusCode = statuscode

        ' Write response
        response.OutputStream.Write(buffer, 0, buffer.Length)
        response.OutputStream.Close()

        UpdateLogText($"Server response: {text}")

    End Sub

    Private Sub _BG_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles _BG.RunWorkerCompleted
        LockUnlock()
        UpdateLogText("Idle ... ")
    End Sub

    Private Sub _BG_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles _BG.ProgressChanged

    End Sub

    Private Sub NotifyIcon1_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles NotifyIcon1.MouseDoubleClick
        HideToTray()
    End Sub

End Class
