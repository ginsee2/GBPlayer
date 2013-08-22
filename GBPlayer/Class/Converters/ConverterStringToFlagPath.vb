Imports System.IO
Imports System.Reflection

'CONVERTER POUR AFFICHAGE DES DRAPEAUX
Public NotInheritable Class ConverterStringToFlagPath
    Implements IValueConverter
    Private Const GBAU_NOMDOSSIER_IMAGESPAYS = "GBDev\GBPlayer\Images\States"
    Private PathFichier As String
    Public Function GetPathImage(ByVal NomImage As String) As String
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_IMAGESPAYS
        If Not Directory.Exists(RepDest) Then
            Directory.CreateDirectory(RepDest)
        End If
        PathFichier = RepDest & "\" & NomImage
        If Not File.Exists(PathFichier) Then
            Try
                Dim s As Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("gbDev." & NomImage)
                Dim b As FileStream = New FileStream(PathFichier, FileMode.Create)
                s.CopyTo(b)
                b.Close()
            Catch ex As Exception
                Debug.Print(ex.Message)
                Return Nothing
            End Try
        End If
        Return PathFichier
    End Function
    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        If value.ToString <> "" Then
            Return GetPathImage(value.ToString & ".png") 'TabParam & value.ToString & ".png"
        Else
            Return Nothing
        End If
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class

