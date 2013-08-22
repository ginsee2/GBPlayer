Imports System.IO
Imports System.Reflection

'CONVERTER POUR AFFICHAGE DES IMAGES
Public NotInheritable Class ConverterIdReleaseToImagePath
    Implements IMultiValueConverter

    Private Const GBAU_NOMDOSSIER_IMAGESMP3 = "GBDev\GBPlayer\Images\mp3"
    Public Function Convert(ByVal values() As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IMultiValueConverter.Convert
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_IMAGESMP3
        If values(0) IsNot Nothing Then
            If values(0).ToString <> "" Then
                Dim Fichier As String = RepDest & "\" & values(0).ToString & ".jpg"
                If File.Exists(RepDest & "\" & values(0).ToString & ".jpg") And values(0).ToString <> "-1" Then
                    Return New BitmapImage(New Uri(Fichier, UriKind.RelativeOrAbsolute))
                End If
            End If
        End If
        If values(1) Is Nothing Then Return Nothing
        If values(1) = "" Then Return Nothing
        Return New BitmapImage(New Uri("../Images/ImageVinylVierge.png", UriKind.RelativeOrAbsolute))
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetTypes() As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object() Implements System.Windows.Data.IMultiValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function

End Class