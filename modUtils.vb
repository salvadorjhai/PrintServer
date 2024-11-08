Imports System.IO
Imports System.Text

Module modUtils


End Module


Public Module ConsoleWriter
    Public Enum logTypeEnum
        system
        normal
        info
        highlight
        sucess
        danger
    End Enum

    Dim _WriteToLogLock As New Object


    Private Function GetLogFileName() As String
        Dim LogFilename As String = ".\logs\" & Now.ToString("yyyy-MM-dd") & ".txt"
        ' check/create new log file if limit exceeds
        If File.Exists(LogFilename) Then
            Dim limit = 100 'Mb limit
            Dim sz = (My.Computer.FileSystem.GetFileInfo(LogFilename).Length / 1024) / 1024
            If sz > limit Then
                Dim ctl = 2
                Do While True
                    LogFilename = ".\logs\" & Now.ToString("yyyy-MM-dd") & $"_{ctl}.txt"
                    If File.Exists(LogFilename) = False Then Exit Do
                    sz = (My.Computer.FileSystem.GetFileInfo(LogFilename).Length / 1024) / 1024
                    If sz < limit Then Exit Do
                    ctl += 1
                Loop
            End If
        End If
        Return LogFilename
    End Function

    Sub WriteToLog(text As String, Optional t As logTypeEnum = logTypeEnum.normal)
        If String.IsNullOrWhiteSpace(text) Then
            Console.WriteLine()
            Return
        End If

        SyncLock _WriteToLogLock
            If Directory.Exists(".\logs") = False Then Directory.CreateDirectory(".\logs")

            Dim LogFilename = GetLogFileName()
            text = $"[{Now.ToString("yyyy-MM-dd HH:mm:ss")}] - {text}"
            File.AppendAllLines(LogFilename, {text}, New UTF8Encoding(False))

            Select Case t
                Case logTypeEnum.danger
                    WriteError(text)
                Case logTypeEnum.highlight
                    WriteHighlight(text)
                Case logTypeEnum.info
                    WriteInfo(text)
                Case logTypeEnum.normal
                    WriteNormal(text)
                Case logTypeEnum.sucess
                    WriteSucess(text)
                Case logTypeEnum.system
                    WriteSystem(text)
            End Select

        End SyncLock
    End Sub

    Sub WriteToLog(ex As Exception, Optional t As logTypeEnum = logTypeEnum.danger)
        SyncLock _WriteToLogLock
            If Directory.Exists(".\logs") = False Then Directory.CreateDirectory(".\logs")

            Dim str As New StringBuilder
            str.AppendLine(ex.Message)
            str.AppendLine(ex.StackTrace)
            If ex.InnerException IsNot Nothing Then
                str.AppendLine(vbTab & ex.InnerException.Message)
                str.AppendLine(vbTab & ex.InnerException.StackTrace)
            End If

            Dim LogFilename = GetLogFileName()
            Dim text = $"[{Now.ToString("yyyy-MM-dd HH:mm:ss")}] - {str}"
            File.AppendAllLines(LogFilename, {text}, New UTF8Encoding(False))

            Select Case t
                Case logTypeEnum.danger
                    WriteError(text)
                Case logTypeEnum.highlight
                    WriteHighlight(text)
                Case logTypeEnum.info
                    WriteInfo(text)
                Case logTypeEnum.normal
                    WriteNormal(text)
                Case logTypeEnum.sucess
                    WriteSucess(text)
                Case logTypeEnum.system
                    WriteSystem(text)
            End Select

        End SyncLock
    End Sub

    Sub WriteSystem(text As String)
        If String.IsNullOrWhiteSpace(text) Then
            Console.WriteLine()
            Return
        End If

        Dim def = Console.ForegroundColor
        Console.ForegroundColor = ConsoleColor.Yellow
        Console.WriteLine(text)
        Console.ForegroundColor = def
    End Sub

    Sub WriteNormal(text As String)
        If String.IsNullOrWhiteSpace(text) Then
            Console.WriteLine()
            Return
        End If
        Console.WriteLine(text)
    End Sub

    Sub WriteInfo(text As String)
        If String.IsNullOrWhiteSpace(text) Then
            Console.WriteLine()
            Return
        End If
        Dim def = Console.ForegroundColor
        Console.ForegroundColor = ConsoleColor.DarkCyan
        Console.WriteLine(text)
        Console.ForegroundColor = def
    End Sub

    Sub WriteHighlight(text As String)
        If String.IsNullOrWhiteSpace(text) Then
            Console.WriteLine()
            Return
        End If
        Dim def = Console.ForegroundColor
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine(text)
        Console.ForegroundColor = def
    End Sub

    Sub WriteSucess(text As String)
        If String.IsNullOrWhiteSpace(text) Then
            Console.WriteLine()
            Return
        End If
        Dim def = Console.ForegroundColor
        Console.ForegroundColor = ConsoleColor.DarkGreen
        Console.WriteLine(text)
        Console.ForegroundColor = def
    End Sub

    Sub WriteError(text As String)
        If String.IsNullOrWhiteSpace(text) Then
            Console.WriteLine()
            Return
        End If
        Dim def = Console.ForegroundColor
        Console.ForegroundColor = ConsoleColor.DarkRed
        Console.WriteLine(text)
        Console.ForegroundColor = def
    End Sub

End Module

