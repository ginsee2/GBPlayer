Option Compare Text
Imports System.Text
Imports System.Net
Imports System.IO
Imports System.Runtime.InteropServices

Public Class FreeServer
    Const GBAU_UserAgent = "GBPLayer3.0 2012 VB.net / Application Discogs API v2"
    Public Shared Property Proxy() As WebProxy
    Public Shared Function Inscription(mail As String, guid As String) As String
        Dim freeServerUrl As String = "http://gbdev.free.fr/gbdvs/inscription.php"
        Dim data As String = "mail=" & EncodeParameterString(mail) & "&" &
                            "guid=" & EncodeParameterString(guid)
        Dim Resultat As String = Requete(freeServerUrl, data)
        Dim tabResultat As String() = Split(Resultat, Chr(13))
        If InStr(tabResultat(0), "Error") > 1 Then
            Return ""
        Else
            Return Trim(ExtraitChaine(tabResultat(0), "=", ""))
        End If
    End Function
    Public Shared Function TestValidUser(mail As String, guid As String, ByRef userID As String) As Dictionary(Of String, String)
        Dim freeServerUrl As String = "http://gbdev.free.fr/gbdvs/validiteversion.php"
        Dim data As String = "mail=" & EncodeParameterString(mail) & "&" &
                            "guid=" & EncodeParameterString(guid) & "&" &
                            "userID=" & EncodeParameterString(userID)
        Dim Resultat As String = Requete(freeServerUrl, data)
        Dim tabResultat As String() = Split(Resultat, Chr(13))
        Dim DicRetour As New Dictionary(Of String, String)
        For Each i In tabResultat
            Dim tabElement As String() = Split(i, "=")
            If tabElement.Count > 1 Then DicRetour.Add(Trim(tabElement(0)), Trim(tabElement(1)))
        Next
        Return DicRetour
    End Function
    Private Shared Function Requete(freeServerUrl As String, data As String) As String
        Dim req As HttpWebRequest = System.Net.WebRequest.Create(freeServerUrl)
        If Proxy IsNot Nothing Then req.Proxy = Proxy
        req.Method = "POST"
        req.ServicePoint.Expect100Continue = False
        req.UserAgent = GBAU_UserAgent
        req.ContentType = "application/x-www-form-urlencoded"
        req.Headers.Add("Accept-Encoding", "gzip")
        req.AutomaticDecompression = Net.DecompressionMethods.GZip
        Dim encoding As New ASCIIEncoding
        Dim TabBytes As Byte() = encoding.GetBytes(data)
        req.ContentLength = TabBytes.Length
        Dim resp As HttpWebResponse = Nothing
        Try
            Dim Writer As Stream = req.GetRequestStream
            Writer.Write(TabBytes, 0, TabBytes.Length)
            Writer.Close()

            resp = DirectCast(req.GetResponse(), HttpWebResponse)
            Dim sr As New StreamReader(resp.GetResponseStream())
            Dim chaine = sr.ReadToEnd()
            Return chaine
        Catch ex As Exception
            Return "Error : " & ex.Message
        Finally
            If resp IsNot Nothing Then resp.Close()
        End Try
    End Function
    Private Shared Function EncodeParameterString(ByVal val As [String]) As [String]
        Dim sb As New StringBuilder()
        For Each c As Char In val
            'ALPHA
            'ALPHA
            'DIGIT
            '"-"
            '"."
            '"_"
            If ("A"c <= c AndAlso c <= "Z"c) OrElse ("a"c <= c AndAlso c <= "z"c) OrElse ("0"c <= c AndAlso c <= "9"c) _
                OrElse c = "-"c OrElse c = "."c OrElse c = "_"c OrElse c = "~"c Then
                '"~"
                sb.Append(c)
            Else
                Dim encoded As Byte() = Encoding.UTF8.GetBytes(New Char() {c})
                For i As Integer = 0 To encoded.Length - 1
                    sb.Append("%"c)
                    sb.Append(encoded(i).ToString("X2"))
                Next
            End If
        Next
        Return sb.ToString()
    End Function


End Class
