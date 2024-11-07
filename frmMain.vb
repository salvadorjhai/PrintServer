Imports System
Imports System.Net
Imports System.IO
Imports System.Drawing.Printing
Imports Newtonsoft.Json.Linq
Imports Newtonsoft.Json
Imports System.Text

Public Class frmMain

    Class config
        Property listening_port As Integer = -1
        Property printer1 As String = ""
        Property printer2 As String = ""
        Property printer3 As String = ""

        Private Shared fl As String = ".\appconfig.js"
        Shared Function load() As config
            If File.Exists(fl) Then
                Return JsonConvert.DeserializeObject(Of config)(File.ReadAllText(fl, New UTF8Encoding(False)))
            End If
            Return New config
        End Function

        Function Save() As config
            Dim js = JsonConvert.SerializeObject(Me, Formatting.Indented)
            File.WriteAllText(fl, js, New UTF8Encoding(False))
            Return Me
        End Function
    End Class

    Private APP_CONFIG As config = Nothing

    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        APP_CONFIG = config.load()

        Me.CenterToScreen()

        loadPrinters()

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

    End Sub


End Class
