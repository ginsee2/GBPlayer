Option Compare Text
Imports System.IO
Imports System.Xml
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Threading

Public Class userControlDiscogsInfos
    Private Property MemIdRelease As String = "N/A"
    Private Property MemIdMaster As String = "N/A"
    Private Discogs As Discogs
    Public Property IdRelease As String = ""

    '**************************************************************************************************************
    '**********************************FONCTIONS PUBLIQUES DE GESTION MISE A JOUR FENETE***************************
    '**************************************************************************************************************
    Shared MemErreur As List(Of String) = New List(Of String)
    Dim DemandeMaJ As Long
    Private Delegate Sub NoArgDelegate()
    Private Delegate Sub addImageDelegate(ByVal idMaJ As Integer, ByVal urlImage As gbDev.DiscogsImage)
    Private Delegate Sub addTexteDelegate(ByVal urlImage As String)
    Private Delegate Sub IdChangedDelegate(ByVal id As String, ByVal NumMiseAJour As Long)
    Private Delegate Sub MiseAJourAffichageDelegate(ByVal newuri As Uri, ByVal id As String, ByVal NumMiseAJour As Integer)
    Public Sub UpdateRelease(ByVal id As String, ByVal newdiscogs As Discogs)
        Discogs = newdiscogs
        IdRelease = id
        PageMaster.Visibility = Windows.Visibility.Hidden
        If (PageRelease.Visibility <> Windows.Visibility.Visible) Or (id <> MemIdRelease) Or (id = "") Then
            DemandeMaJ += 1
            Dim ReadTag As New IdChangedDelegate(AddressOf ReleaseIdChanged)
            ReadTag.BeginInvoke(id, DemandeMaJ, Nothing, Nothing)
            MemIdRelease = id
        End If
    End Sub
    Public Sub UpdateMaster(ByVal id As String, ByVal newdiscogs As Discogs)
        Discogs = newdiscogs
        ' Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
        '                               New NoArgDelegate(Sub()
        PageRelease.Visibility = Windows.Visibility.Hidden
        IdRelease = ""
        If (PageMaster.Visibility <> Windows.Visibility.Visible) Or (id <> MemIdMaster) Then
            Dim newuri As Uri = New Uri(Discogs.getXmlMaster(id))
            Dim uriReleases As Uri = New Uri(Discogs.getXmlMasterReleases(id, 1))
            If Split(newuri.LocalPath, "\").Last <> "Discogsmaster.xml" Then
                PageErreur.Visibility = Visibility.Hidden
                PageMaster.Visibility = Visibility.Visible
            Else
                PageErreur.Visibility = Visibility.Visible
                PageMaster.Visibility = Visibility.Hidden
                ErreurDataProvider.Source = New Uri(Discogs.GetXmlErreurFileName())
            End If
            'ErreurDataProvider.Source = Nothing
            'ErreurDataProvider.Source = New Uri(DiscogsInfos.GetXmlErreurFileName())
            'DataProvider.Source = Nothing
            DataProviderMasterReleases.Source = uriReleases
            DataProvider.Source = newuri
            MemIdMaster = id
        End If
        '                                                End Sub))
    End Sub
    Private Sub ExpanderMasterReleases_Expanded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles ExpanderMasterReleases.Expanded
        If (PageRelease.Visibility = Windows.Visibility.Visible) And (MemIdRelease <> "") Then
            'Dim newuri As Uri = New Uri(Discogs.getXmlMaster(tagLinkMaster.Text))
            Dim uriReleases As Uri = New Uri(Discogs.getXmlMasterReleases(tagLinkMaster.Text, 1))
            DataProviderMasterVersions.Source = uriReleases
            MemIdMaster = tagLinkMaster.Text
        End If
    End Sub

    Private Sub ReleaseIdChanged(ByVal id As String, ByVal NumMiseAJour As Integer)
        If NumMiseAJour <> DemandeMaJ Then Exit Sub
        Dim newuri As Uri = New Uri(Discogs.getXmlRelease(id, False))
        If NumMiseAJour = DemandeMaJ Then
            Me.Dispatcher.BeginInvoke(New MiseAJourAffichageDelegate(Sub(uri As Uri, idEnCours As String, idMaJ As Integer)
                                                                         If (idMaJ = DemandeMaJ) Then
                                                                             If Split(uri.LocalPath, "\").Last <> "Discogsrelease.xml" Then
                                                                                 PageErreur.Visibility = Visibility.Hidden
                                                                                 PageRelease.Visibility = Visibility.Visible
                                                                             Else
                                                                                 PageErreur.Visibility = Visibility.Visible
                                                                                 PageRelease.Visibility = Visibility.Hidden
                                                                                 ErreurDataProvider.Source = New Uri(Discogs.GetXmlErreurFileName())
                                                                             End If
                                                                             ExpanderMasterReleases.IsExpanded = False
                                                                             DataProviderMasterVersions.Source = Nothing
                                                                             DataProvider.Source = uri
                                                                             DataProvider.Refresh()
                                                                         End If
                                                                     End Sub),
                                        System.Windows.Threading.DispatcherPriority.Background, {newuri, id, NumMiseAJour})
        End If
        Me.Dispatcher.BeginInvoke(New addTexteDelegate(Sub(NbrImages As String)
                                                           tagImages.Items.Clear()
                                                           Dim Compteur As Integer = CInt(NbrImages)
                                                           Dim CompteurImages As TextBlock = New TextBlock
                                                           CompteurImages.Name = "tagImageCount"
                                                           CompteurImages.Foreground = Brushes.Blue
                                                           CompteurImages.Background = Brushes.Transparent
                                                           CompteurImages.Text = Compteur & " Image" & IIf(Compteur > 1, "s...", "...")
                                                           CompteurImages.HorizontalAlignment = AlignmentX.Center
                                                           tagImages.Items.Add(CompteurImages)
                                                       End Sub),
                                    System.Windows.Threading.DispatcherPriority.Background, {Discogs.release.images.Count.ToString})
        For Each i In Discogs.release.images
            If NumMiseAJour <> DemandeMaJ Then Exit Sub
            Dim RepDest = Discogs.GetRepertoireImages
            Dim NomFichier As String = Split(i.urlImage, "/").Last
            Dim CheminAccesImage As String = ""
            If NomFichier <> "" Then
                If (File.Exists(RepDest & "\" & NomFichier)) And (NomFichier <> "-1") Then
                    ' CheminAccesImage = RepDest & "\" & NomFichier
                Else
                    CheminAccesImage = i.urlImage
                End If
            End If
            If CheminAccesImage <> "" Then
                If Not MemErreur.Contains(CheminAccesImage) Then
                    Try
                        Dim bi3 As New BitmapImage
                        bi3.BeginInit()
                        bi3.StreamSource = New MemoryStream(TagID3.tagID3Object.FonctionUtilite.UploadImage(CheminAccesImage)) ' New Uri(CheminAccesImage, UriKind.RelativeOrAbsolute)
                        bi3.EndInit()
                        If Not File.Exists(RepDest & "\" & NomFichier) Then
                            TagID3.tagID3Object.FonctionUtilite.SaveImage(bi3, RepDest & "\" & NomFichier)
                        End If
                    Catch ex As Exception
                        MemErreur.Add(CheminAccesImage)
                    End Try
                End If
            End If
            If NumMiseAJour <> DemandeMaJ Then Exit Sub
            Me.Dispatcher.BeginInvoke(New addImageDelegate(Sub(idMaJ As Integer, urlImage As gbDev.DiscogsImage)
                                                               If idMaJ <> DemandeMaJ Then Exit Sub
                                                               Dim Element As Border = addImage(urlImage)
                                                               tagImages.Items.Insert(tagImages.Items.Count - 1, (Element))
                                                           End Sub),
                                        System.Windows.Threading.DispatcherPriority.Background, {NumMiseAJour, i})
        Next
    End Sub

    Private Function addImage(ByVal urlImage As gbDev.DiscogsImage) As Border
        Dim uriImage As String = urlImage.urlImage
        Dim hauteur As String = urlImage.hauteur
        Dim largeur As String = urlImage.largeur
        If uriImage <> "" Then
            Dim InfosImages As ToolTip = New ToolTip
            Dim ImageToolTip As Image = New Image
            ImageToolTip.Height = 300
            ImageToolTip.Width = 300
            ImageToolTip.Stretch = Stretch.Fill
            ImageToolTip.Source = DownloadBitmapImage(uriImage)
            ImageToolTip.Style = CType(Me.FindResource("GBImage"), Style)
            ImageToolTip.Margin = New Thickness(2)
            InfosImages.Content = ImageToolTip
            Dim ImageDisque As Image = New Image
            ImageDisque.ToolTip = InfosImages
            ImageDisque.Height = 70
            ImageDisque.Width = 70
            ImageDisque.Stretch = Stretch.Fill
            ImageDisque.Source = DownloadBitmapImage(uriImage)
            ImageDisque.Style = CType(Me.FindResource("GBImage"), Style)
            ImageDisque.Name = "iDiscogs"
            ImageDisque.AllowDrop = True
            Dim BordureImage As Border = New Border
            BordureImage.Margin = New Thickness(2)
            BordureImage.Child = ImageDisque
            Return BordureImage
        End If
        Return Nothing
    End Function
    Private Function DownloadBitmapImage(ByVal NomImage As String) As BitmapImage
        Dim RepDest = Discogs.GetRepertoireImages
        Dim NomFichier As String = Split(NomImage, "/").Last
        Dim CheminAccesImage As String = ""
        If NomFichier <> "" Then
            If (File.Exists(RepDest & "\" & NomFichier)) And (NomFichier <> "-1") Then
                CheminAccesImage = RepDest & "\" & NomFichier
            Else
                Return Nothing
            End If
        Else
            Return Nothing
        End If
        If CheminAccesImage <> "" Then
            Dim bi3 As New BitmapImage
            Try
                bi3.BeginInit()
                Dim StreamData() As Byte = TagID3.tagID3Object.FonctionUtilite.UploadImage(CheminAccesImage)
                bi3.StreamSource = New MemoryStream(StreamData) ' New Uri(CheminAccesImage, UriKind.RelativeOrAbsolute)
                bi3.EndInit()
            Catch ex As Exception
            End Try
            Return bi3
        End If
    End Function

    '**************************************************************************************************************
    '**************************************GESTION DU DRAG AND DROP************************************************
    '**************************************************************************************************************
    'Dim StartPoint As Point
    'Dim TypeObjetClic As Object
    'Dim PlateformVista As Boolean
    Dim TexteLink As TextBlock = Nothing

    Private Sub me_PreviewMouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonDown
        'StartPoint = e.GetPosition(XMLBinding)
        'TypeObjetClic = e.OriginalSource
        'If (TypeOf (e.OriginalSource) Is ScrollViewer) Then XMLBinding.SelectedItems.Clear()
        If TypeOf (e.OriginalSource) Is TextBlock Then
            If CType(e.OriginalSource, TextBlock).Name Like "tagLink*" Then
                TexteLink = CType(e.OriginalSource, TextBlock)
                Select Case TexteLink.Name
                    Case "tagLinkRelease"
                        Dim Contenu As XmlElement = DirectCast(TexteLink.Tag, XmlElement)
                        If Contenu IsNot Nothing Then
                            UpdateRelease(Contenu.InnerText, Discogs)
                        End If
                    Case "tagLinkArtiste"
                        Dim Contenu As XmlElement = DirectCast(TexteLink.Tag, XmlElement)
                        If Contenu IsNot Nothing Then wpfMsgBox.MsgBoxInfo("Appel artiste", Contenu.InnerText)
                    Case "tagLinkLabel"
                        Dim Contenu As XmlElement = DirectCast(TexteLink.Tag, XmlElement)
                        If Contenu IsNot Nothing Then wpfMsgBox.MsgBoxInfo("Appel label", Contenu.InnerText)
                    Case "tagLinkMaster"
                        Dim Contenu As XmlElement = DirectCast(TexteLink.Tag, XmlElement)
                        If Contenu IsNot Nothing Then
                            UpdateMaster(Contenu.InnerText, Discogs)
                        End If
                        'wpfMsgBox.MsgBoxInfo("Appel master", Discogs.getXmlMaster(Contenu.InnerText))
                End Select
                'lancement procedure de recherche
                e.Handled = True
            End If
        End If
        '  wpfMsgBox.MsgBoxInfo("INFO", e.OriginalSource.ToString, Nothing)
    End Sub
    Private Sub me_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles Me.MouseMove
        If TexteLink IsNot Nothing Then
            TexteLink.Cursor = Cursors.Arrow
            TexteLink.Foreground = Brushes.Black
            TexteLink = Nothing
        End If
        If TypeOf (e.OriginalSource) Is TextBlock Then
            If (CType(e.OriginalSource, TextBlock).Name Like "tagLink*") Then
                TexteLink = CType(e.OriginalSource, TextBlock)
                TexteLink.Cursor = Cursors.Hand
                If (Keyboard.GetKeyStates(Key.LeftShift) And KeyStates.Down) > 0 Then
                    TexteLink.Foreground = Brushes.Red
                Else
                    TexteLink.Foreground = Brushes.Blue
                End If
            End If
        End If
        'End If
    End Sub

End Class