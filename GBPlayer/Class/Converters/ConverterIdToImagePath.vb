Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Xml

'CONVERTER POUR AFFICHAGE DES IMAGES
Public NotInheritable Class ConverterIdToImagePath
    Implements IValueConverter
    Private Const GBAU_NOMDOSSIER_IMAGESVINYLS = "GBDev\GBPlayer\Images\Vinyls"
    Private Delegate Sub ImageLoaderDelegate(ByVal id As String, ImageAAfficher As Image, Item As ListViewItem)
    Private Delegate Sub UpdateImage(ByVal NomImage As String, ImageDestination As Image)
    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_IMAGESVINYLS
        If Not (TypeOf value Is String) Then
            'If (wpfApplication.FindAncestor(value, "ListViewItem")) IsNot Nothing Then
            Dim ImageAAfficher As Image = value
            Dim ItemSurvole As ListViewItem = wpfApplication.FindAncestor(value, "ListViewItem")
            Dim DonneeSurvolee As XmlElement = CType(ItemSurvole.Content, XmlElement)
            Dim NomImage As String = DonneeSurvolee.SelectSingleNode("Image").InnerText
            Dim Pays As String = DonneeSurvolee.SelectSingleNode("Pays").InnerText
            If (File.Exists(RepDest & "\" & NomImage & ".jpg") And NomImage <> "-1" And Pays <> "Achercher") Then
                Dim bi1 As New BitmapImage
                bi1.BeginInit()
                bi1.UriSource = New Uri(RepDest & "\" & NomImage & ".jpg", UriKind.Absolute)
                bi1.EndInit()
                Dim InfosImages As ToolTip = New ToolTip
                Dim ImageToolTip As Image = New Image
                ImageToolTip.Height = 150
                ImageToolTip.Width = 150
                ImageToolTip.Stretch = Stretch.Fill
                ImageToolTip.Source = bi1
                ImageToolTip.Style = CType(ImageAAfficher.FindResource("GBImage"), Style)
                ImageToolTip.Margin = New Thickness(2)
                InfosImages.Content = ImageToolTip
                ImageAAfficher.ToolTip = InfosImages
                Return RepDest & "\" & NomImage & ".jpg"
            Else
                NomImage = DonneeSurvolee.SelectSingleNode("Image").InnerText
                Dim ReadTag As New ImageLoaderDelegate(AddressOf ChargeImageDiscogs)
                ReadTag.BeginInvoke(NomImage, ImageAAfficher, ItemSurvole, Nothing, Nothing)
                Return RepDest & "\" & NomImage & ".jpg"
            End If
            Return Nothing
        End If
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
    Private Function ChargeImageDiscogs(ByVal id As String, ImageAAfficher As Image, Item As ListViewItem) As Boolean
        Dim PathFichier As String
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_IMAGESVINYLS
        If Not Directory.Exists(RepDest) Then
            Directory.CreateDirectory(RepDest)
        End If
        PathFichier = RepDest & "\" & id & ".jpg"
        Dim RequeteDiscogs As Discogs = New Discogs(id)
        If RequeteDiscogs.release.artiste.nom <> "" Then
            If RequeteDiscogs.release.images.Count > 0 Then
                Try
                    Dim WebClient As New System.Net.WebClient
                    WebClient.Headers.Add("user-agent", "GBPlayer3")
                    Dim bi3 As New BitmapImage
                    bi3.BeginInit()
                    bi3.StreamSource = New MemoryStream(WebClient.DownloadData(New Uri(RequeteDiscogs.release.images(0).urlImage))) ' New Uri(CheminAccesImage, UriKind.RelativeOrAbsolute)
                    bi3.EndInit()
                    If Not File.Exists(PathFichier) Then
                        TagID3.tagID3Object.FonctionUtilite.SaveImage(bi3, PathFichier)
                    End If
                    ImageAAfficher.Dispatcher.BeginInvoke(New UpdateImage(Sub(NomImage As String, ImageDestination As Image)
                                                                              Try
                                                                                  Dim bi1 As New BitmapImage
                                                                                  bi1.BeginInit()
                                                                                  bi1.UriSource = New Uri(NomImage, UriKind.Absolute)
                                                                                  bi1.EndInit()
                                                                                  Dim InfosImages As ToolTip = New ToolTip
                                                                                  Dim ImageToolTip As Image = New Image
                                                                                  ImageToolTip.Height = 150
                                                                                  ImageToolTip.Width = 150
                                                                                  ImageToolTip.Stretch = Stretch.Fill
                                                                                  ImageToolTip.Source = bi1
                                                                                  ImageToolTip.Style = CType(ImageDestination.FindResource("GBImage"), Style)
                                                                                  ImageToolTip.Margin = New Thickness(2)
                                                                                  InfosImages.Content = ImageToolTip
                                                                                  ImageDestination.ToolTip = InfosImages
                                                                                  Dim bi2 As New BitmapImage
                                                                                  bi2.BeginInit()
                                                                                  bi2.UriSource = New Uri(NomImage, UriKind.Absolute)
                                                                                  bi2.EndInit()
                                                                                  ImageDestination.Source = bi2
                                                                                  Dim DonneeSurvolee As XmlElement = CType(Item.Content, XmlElement)
                                                                                  DonneeSurvolee.SelectSingleNode("Pays").InnerText = RequeteDiscogs.release.pays
                                                                                  DonneeSurvolee.SelectSingleNode("Style").InnerText() = RequeteDiscogs.release.style
                                                                                  If DonneeSurvolee.SelectSingleNode("Pistes") IsNot Nothing Then DonneeSurvolee.SelectSingleNode("Pistes").RemoveAll()
                                                                                  For Each i In RequeteDiscogs.release.pistes
                                                                                      Dim NewNode As XmlElement = DonneeSurvolee.OwnerDocument.CreateElement("Piste")
                                                                                      NewNode.InnerText = i.nomPisteConcat
                                                                                      DonneeSurvolee.SelectSingleNode("Pistes").AppendChild(NewNode)
                                                                                  Next
                                                                              Catch ex As Exception
                                                                              End Try
                                                                          End Sub),
                                                System.Windows.Threading.DispatcherPriority.Background, {PathFichier, ImageAAfficher})
                    Return True
                Catch ex As Exception
                End Try
            Else
                ImageAAfficher.Dispatcher.BeginInvoke(New UpdateImage(Sub(NomImage As String, ImageDestination As Image)
                                                                          Try
                                                                              Dim s As Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("gbDev.ImageVinylVierge2.png")
                                                                              Dim b As FileStream = New FileStream(PathFichier, FileMode.Create)
                                                                              s.CopyTo(b)
                                                                              b.Close()
                                                                              Dim bi3 As New BitmapImage
                                                                              bi3.BeginInit()
                                                                              bi3.UriSource = New Uri(PathFichier, UriKind.Absolute)
                                                                              bi3.EndInit()
                                                                              ImageDestination.Source = bi3
                                                                          Catch ex As Exception
                                                                          End Try
                                                                      End Sub),
                                            System.Windows.Threading.DispatcherPriority.Background, {PathFichier, ImageAAfficher})
                Return True
            End If
        End If
        Return False
    End Function
End Class