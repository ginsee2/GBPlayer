Imports System.IO
Imports System.Reflection
Imports System.Threading

'CONVERTER POUR AFFICHAGE DES IMAGES
Public NotInheritable Class ConverterDiscogsIdToImagePath
    Implements IValueConverter
    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim RepDest = Discogs.GetRepertoireImages
        If value IsNot Nothing Then
            If value.ToString <> "" Then
                If File.Exists(RepDest & "\" & Split(value.ToString, "/").Last) And value.ToString <> "-1" Then
                    Return RepDest & "\" & Split(value.ToString, "/").Last
                Else
                    Return DownloadBitmapImage(value.ToString)
                End If
            Else
                Return "/GBPlayer;component/Images/ImageVinylVierge.png"
            End If
        Else
            Return Nothing
        End If
    End Function
    Private Function DownloadBitmapImage(ByVal NomImage As String) As String
        Dim RepDest = Discogs.GetRepertoireImages
        Dim NomFichier As String = RepDest & "/" & Split(NomImage, "/").Last
        If NomImage <> "" Then
            'Dim bi3 As New BitmapImage
            Dim WebClient As New System.Net.WebClient
            WebClient.Headers.Add("user-agent", "GBPlayer3")
            Try
                WebClient.DownloadFile(NomImage, NomFichier)
                'bi3.BeginInit()
                'bi3.UriSource = New Uri(NomFichier, UriKind.RelativeOrAbsolute)
                'bi3.EndInit()
            Catch ex As Exception
            End Try
            Return NomFichier
        End If
    End Function
    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class