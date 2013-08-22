Option Compare Text
'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 06/01/12
'DESCRIPTION : Controle utilisateur pour CD Audio
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.IO
Imports System.Reflection
Imports System.ComponentModel
Imports System.Xml
Imports System.Windows.Controls.Primitives
Imports System.Text
Imports System.Collections.ObjectModel
Imports System.Threading

Public Class userControlCDList

    Public Event SelectionDblClick(ByVal NomFichier As String, ByVal Tag As String)
    '  Public Event RequeteRecherche(ByVal ChaineRecherche As String, ByVal NewRequete As Boolean)
    '  Public Event UpdateLink(ByRef FichierALier As String)
    '  Dim ListeDesRecherches As List(Of String) = New List(Of String)
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Private WithEvents ScanDisque As wpfDrives
    Private ConversionLancee As Boolean
    Private _DiskCollection As New ObservableCollection(Of String)
    Public ReadOnly Property DiskCollection As ObservableCollection(Of String)
        Get
            Return _DiskCollection ' If ScanDisque IsNot Nothing Then Return ScanDisque.DiskCollection Else Return Nothing '
        End Get
    End Property
    Public Property CDRomActif As BassCDDrive
    Public Property DisplayValidation As Boolean
    Dim MenuContextuel As New ContextMenu

    '***********************************************************************************************
    '----------------------------------CONSTRUCTEUR DE LA CLASSE------------------------------------
    '***********************************************************************************************
    Private Sub userControlCDList_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Loaded
        If Not DisplayValidation Then Exit Sub
        Dim ConfigUtilisateur As ConfigPerso = New ConfigPerso
        ConfigUtilisateur = ConfigPerso.LoadConfig
        Dim SauvegardeCD As New Dictionary(Of String, GridViewColumn)
        For Each i As GridViewColumn In CType(XMLBinding.View, GridView).Columns
            SauvegardeCD.Add(CType(i.Header, GridViewColumnHeader).Content.ToString, i)
        Next
        CType(XMLBinding.View, GridView).Columns.Clear()
        ConfigUtilisateur.LISTECD_ListeColonnes.ForEach(Sub(c As String)
                                                            Dim NomColonne As String = ExtraitChaine(c, "", ";")
                                                            Dim Position As Long = CLng(ExtraitChaine(c, ";", "/"))
                                                            Dim Dimension As Double = CDbl(ExtraitChaine(c, "/", ""))
                                                            Dim Colonne As GridViewColumn = SauvegardeCD.Item(NomColonne)

                                                            Colonne.Width = Dimension
                                                            CType(XMLBinding.View, GridView).Columns.Add(Colonne)
                                                        End Sub)

        ActiveTri(ExtraitChaine(ConfigUtilisateur.LISTECD_ColonneTriee, "", ";"),
                  IIf(ExtraitChaine(ConfigUtilisateur.LISTECD_ColonneTriee, ";", "") = "A",
                      ListSortDirection.Ascending, ListSortDirection.Descending))
        If System.Environment.OSVersion.Platform = PlatformID.Win32NT Then
            If System.Environment.OSVersion.Version.Major > 5 Then PlateformVista = True
        End If
        If Not DesignerProperties.GetIsInDesignMode(Me) Then MiseAJourCD()
    End Sub

    '***********************************************************************************************
    '----------------------------------DESTRUCTION DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Private Sub userControlCDList_Unloaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Unloaded
        If Not DisplayValidation Then Exit Sub
        Dim ConfigUtilisateur As ConfigPerso = New ConfigPerso
        ConfigUtilisateur = ConfigPerso.LoadConfig
        SaveConfiguration(ConfigUtilisateur)
        ConfigPerso.SaveConfig(ConfigUtilisateur)
        Dispose()
    End Sub
    Public Sub SaveConfiguration(ByVal ConfigUtilisateur As gbDev.ConfigPerso)
        If Not DisplayValidation Then Exit Sub
        ConfigPerso.UpdateListeColonnes(ConfigUtilisateur.LISTECD_ListeColonnes, CType(XMLBinding.View, GridView).Columns)
        If ColonneTriEnCours IsNot Nothing Then
            ConfigUtilisateur.LISTECD_ColonneTriee = ColonneTriEnCours.Tag & ";" &
                                    IIf(IconeDeTriEnCours.Direction = ListSortDirection.Ascending, "A", "D")
        End If
        Dispose()
    End Sub
    Private Sub Dispose()
        If ScanDisque IsNot Nothing Then
            ScanDisque.Dispose()
            ScanDisque = Nothing
        End If
        If CDRomActif IsNot Nothing Then
            CDRomActif.Dispose()
            CDRomActif = Nothing
        End If
    End Sub

    '***********************************************************************************************
    '----------------------------------GESTION MISE A JOUR DU CONTROL-------------------------------------
    '***********************************************************************************************
    Private Sub MiseAJourCD()
        If ScanDisque Is Nothing Then ScanDisque = New wpfDrives(wpfDrives.EnumDriveUpdate.CDRomModification)
        Dim ListeCDROM As List(Of String) = ScanDisque.GetCDRomDrives
        If ListeCDROM.Count > 0 Then
            DiskCollection.Clear()
            For Each i In ListeCDROM
                Dim Info As DriveInfo = New DriveInfo(i)
                If Info.IsReady Then
                    _DiskCollection.Add(i & " [" & Info.VolumeLabel & "]")
                Else
                    _DiskCollection.Add(i & " [non disponible]")
                End If
            Next
            tagDrives.Text = DiskCollection.First
            If CDRomActif IsNot Nothing Then CDRomActif.Dispose()
            CDRomActif = New BassCDDrive(ListeCDROM.First)
        End If
        If CDRomActif.DriveIsReady Then
            Dim CdDisk As BassCDDisc = CDRomActif.GetDisc
            If CdDisk IsNot Nothing Then
                If CdDisk.DiscIsAudio Then
                    Dim doc = New XmlDocument()
                    doc.LoadXml(CDRomActif.GetDisc.DiscTracks.ToString())
                    DataProvider.Document = doc
                    Exit Sub
                End If
            End If
        End If
        DataProvider.Document = Nothing
    End Sub
    Private Sub ScanDisque_DiskCDRomEjected(ByVal Name As String) Handles ScanDisque.DiskCDRomEjected
        Debug.Print("CDRomActif ejected :" & Name)
        MiseAJourCD()
    End Sub
    Private Sub ScanDisque_DiskCDRomInsered(ByVal Name As String) Handles ScanDisque.DiskCDRomInsered
        Debug.Print("cdrom insered :" & Name)
        MiseAJourCD()
    End Sub

    'PROCEDURE DE GESTION DU MENU D'ENTETE DE LA LISTE
    Private Sub MenuContextuelHeaderListe_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles MenuContextuelHeaderListe.Loaded
        If TypeOf (XMLBinding.View) Is GridView Then
            MenuContextuelHeaderListe.Items.Clear()
            For Each i As GridViewColumn In CType(XMLBinding.View, GridView).Columns
                If CType(i.Header, GridViewColumnHeader).Content.ToString <> "Artiste" Then
                    Dim UnItem As MenuItem = New MenuItem()
                    UnItem.Header = CType(i.Header, GridViewColumnHeader).Content.ToString
                    UnItem.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf ReponseMenuHeader))
                    If i.Width <> 0 Then UnItem.IsChecked = True
                    MenuContextuelHeaderListe.Items.Add(UnItem)
                End If
            Next
        End If
    End Sub
    Public Sub ReponseMenuHeader(ByVal Sender As Object, ByVal e As RoutedEventArgs)
        For Each i As GridViewColumn In CType(XMLBinding.View, GridView).Columns
            If CType(i.Header, GridViewColumnHeader).Content.ToString = CType(Sender, MenuItem).Header Then
                If CType(Sender, MenuItem).IsChecked Then i.Width = 0 Else i.Width = Double.NaN
                Exit For
            End If
        Next
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DES ACTIONS SUR LA LISTE DES TRACKS-------------------
    '***********************************************************************************************
    'PROCEDURE DE REPONSE POUR LES DOUBLES CLICS SUR LA LISTE
    Private Sub XMLBinding_PreviewMouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles XMLBinding.MouseDoubleClick
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As XmlElement = Nothing
        XMLBinding.ContextMenu = Nothing
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            DonneeSurvolee = CType(ItemSurvole.Content, XmlElement)
        End If
        '  If ConversionLancee And Not CDRomActif.DriveIsBusy Then
        'ConversionLancee = False
        If BassCDDrive.DriveUser(CDRomActif.DriveNum) <> BassCDDrive.EnumDriveUser.Converter Then
            If TypeOf (e.OriginalSource) Is Border Then
                If CType(e.OriginalSource, Border).Name = "DragDropBorder" Then
                    RaiseEvent SelectionDblClick(CDRomActif.DriveName & "[" & CDRomActif.DriveNum & "] -" & DonneeSurvolee.SelectSingleNode("Piste").InnerText & ".CD", "")
                End If
                e.Handled = True
                'End If
            End If
        End If
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DES BOUTONS LA LISTE DES PISTES-----------------------
    '***********************************************************************************************
    Private Sub DiscUpdate_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)

    End Sub

    '**************************************************************************************************************
    '**************************************GESTION DU DRAG AND DROP************************************************
    '**************************************************************************************************************
    Dim StartPoint As Point
    Dim TypeObjetClic As Object
    Dim PlateformVista As Boolean
    Private Sub XMLBinding_PreviewMouseLeftButtonUp(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles XMLBinding.PreviewMouseLeftButtonUp
        StartPoint = New Point
    End Sub
    Private Sub XMLBinding_PreviewMouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles XMLBinding.PreviewMouseLeftButtonDown
        StartPoint = e.GetPosition(XMLBinding)
        TypeObjetClic = e.OriginalSource
        If (TypeOf (e.OriginalSource) Is ScrollViewer) Then XMLBinding.SelectedItems.Clear()
        ' UpdateInfosIcons(e.OriginalSource)
        If TypeOf (e.OriginalSource) Is TextBlock Then
            If CType(e.OriginalSource, TextBlock).Name Like "tagLink*" Then
                TexteLink = CType(e.OriginalSource, TextBlock)
                ' UpdateRecherche(sender, e)
                e.Handled = True
            End If
        End If
    End Sub
    Private Sub XMLBinding_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles XMLBinding.MouseLeave
        StartPoint = New Point
    End Sub
    Dim TexteLink As TextBlock = Nothing
    Private Sub XMLBinding_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles XMLBinding.MouseMove
        If TexteLink IsNot Nothing Then
            TexteLink.Cursor = Cursors.Arrow
            TexteLink.Foreground = Brushes.Black
            TexteLink = Nothing
        End If
        If TypeOf (e.OriginalSource) Is Border And TypeOf (TypeObjetClic) Is Border Then
            If CType(e.OriginalSource, Border).Name = "DragDropBorder" Then
                Dim ItemComplet As Grid = CType(wpfApplication.FindAncestor(e.OriginalSource, "Grid"), Grid)
                Dim MousePos As Point = e.GetPosition(XMLBinding)
                Dim Diff As Vector = StartPoint - MousePos
                If StartPoint.X <> 0 And StartPoint.Y <> 0 And (e.LeftButton = MouseButtonState.Pressed And Math.Abs(Diff.X) > 2 Or
                                                                Math.Abs(Diff.Y) > 2) Then
                    Dim ItemSurvole As ListViewItem = Nothing
                    Dim DonneeSurvolee As XmlElement = Nothing
                    Dim FilesDataObject(XMLBinding.SelectedItems.Count - 1) As String
                    Dim FilesTextName As String = ""
                    If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
                        ItemSurvole = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
                        DonneeSurvolee = CType(ItemSurvole.Content, XmlElement)
                    End If
                    For i = 0 To XMLBinding.SelectedItems.Count - 1
                        DonneeSurvolee = CType(XMLBinding.SelectedItems(i), XmlElement)
                        FilesDataObject(i) = CDRomActif.DriveName & "[" & CDRomActif.DriveNum & "] -" & DonneeSurvolee.SelectSingleNode("Piste").InnerText & ".CD"
                        If FilesTextName = "" Then
                            FilesTextName = FilesDataObject(i)
                        Else
                            FilesTextName = FilesTextName & ";" & FilesDataObject(i)
                        End If
                    Next
                    If PlateformVista Then
                        Dim TabParametres(1) As KeyValuePair(Of String, Object)
                        TabParametres(0) = New KeyValuePair(Of String, Object)(DataFormats.Text, FilesTextName)
                        TabParametres(1) = New KeyValuePair(Of String, Object)("fileCDAudio", FilesDataObject)
                        DragSourceHelper.DoDragDrop(ItemComplet, e.GetPosition(e.OriginalSource),
                                                    DragDropEffects.Copy, TabParametres)
                    Else
                        Dim data As DataObject = New DataObject()
                        data.SetData(DataFormats.Text, FilesTextName)
                        data.SetData("fileCDAudio", FilesDataObject)
                        Dim effects As DragDropEffects = DragDrop.DoDragDrop(Me, data, DragDropEffects.Copy)
                    End If
                End If
            End If
        Else
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
        End If
    End Sub

    Private Sub ListeFichiers_Drop(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles XMLBinding.Drop
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As XmlElement = Nothing
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            DonneeSurvolee = CType(ItemSurvole.Content, XmlElement)
            CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).Opacity = 0
        End If
        e.Effects = DragDropEffects.None
        If PlateformVista Then DropTargetHelper.Drop(e.Data, e.GetPosition(e.OriginalSource), e.Effects)
        e.Handled = True
    End Sub
    Private Sub ListeFichiers_DragEnter(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles XMLBinding.DragEnter
        Dim FlagPlateformVista As Boolean
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As XmlElement = Nothing
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            DonneeSurvolee = CType(ItemSurvole.Content, XmlElement)
            CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).Opacity = 0.7
        End If
        e.Effects = DragDropEffects.None
        If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                        e.Data, e.GetPosition(e.OriginalSource),
                                                        e.Effects, "Opération impossible", "")
        e.Handled = True
    End Sub
    Private Sub ListeFichiers_DragLeave(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles XMLBinding.DragLeave
        Dim ItemSurvole As ListViewItem
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).Opacity = 0
        End If
        If PlateformVista Then
            DropTargetHelper.DragLeave(e.Data)
        End If
        e.Handled = True
    End Sub
    Private Sub ListeFichiers_DragOver(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles XMLBinding.DragOver
        'Static compteur As Integer
        Dim FlagPlateformVista As Boolean
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        e.Effects = DragDropEffects.None
        If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                            e.Data, e.GetPosition(e.OriginalSource),
                                                            e.Effects, "Opération impossible", "")
        e.Handled = True
    End Sub
    '***********************************************************************************************
    '---------------------------------GESTION DES MISES A JOUR LA LISTE DES VINYLS------------------
    '***********************************************************************************************

    '***********************************************************************************************
    '---------------------------------MISE A JOUR DES ICONES DANS LISTE-----------------------
    '***********************************************************************************************

    '***********************************************************************************************
    '---------------------------------GESTION DU TRI DANS LA LISTE DES VINYLS-----------------------
    '***********************************************************************************************
    Private ColonneTriEnCours As GridViewColumnHeader
    Private IconeDeTriEnCours As AdornerSort
    Private DirectionEnCours As ListSortDirection

    'Procedure de réponse à une demande de tri sur une des colonne de la liste
    Public Sub SortClick(ByVal Sender As Object, ByVal e As RoutedEventArgs)
        Dim Colonne As GridViewColumnHeader = CType(Sender, GridViewColumnHeader)
        Dim ChampATrier As String = Colonne.Tag
        If ColonneTriEnCours IsNot Nothing Then
            AdornerLayer.GetAdornerLayer(Colonne).Remove(IconeDeTriEnCours)
            XMLBinding.Items.SortDescriptions.Clear()
        End If
        Dim NouvelleDirection As ListSortDirection = ListSortDirection.Ascending
        If ColonneTriEnCours IsNot Nothing Then
            If (ColonneTriEnCours.Tag = Colonne.Tag) And (IconeDeTriEnCours.Direction = NouvelleDirection) Then
                NouvelleDirection = ListSortDirection.Descending
            End If
        End If
        ColonneTriEnCours = Colonne
        IconeDeTriEnCours = New AdornerSort(ColonneTriEnCours, NouvelleDirection)
        AdornerLayer.GetAdornerLayer(ColonneTriEnCours).Add(IconeDeTriEnCours)
        XMLBinding.Items.SortDescriptions.Add(New SortDescription(ChampATrier, NouvelleDirection))
        XMLBinding.Items.SortDescriptions.Add(New SortDescription("Artiste", ListSortDirection.Ascending))
        XMLBinding.Items.SortDescriptions.Add(New SortDescription("Titre", ListSortDirection.Ascending))
    End Sub
    'Procedure de réponse à une demande de tri sur une des colonne de la liste
    Public Sub ActiveTri(ByVal ChampATrier As String, Optional ByVal NouvelleDirection As ListSortDirection = ListSortDirection.Ascending)
        Dim Colonne As GridViewColumnHeader = Nothing
        For Each i As GridViewColumn In CType(XMLBinding.View, GridView).Columns
            If CType(i.Header, GridViewColumnHeader).Tag = ChampATrier Then
                Colonne = CType(i.Header, GridViewColumnHeader)
                Exit For
            End If
        Next
        If Colonne Is Nothing Then Exit Sub
        If ColonneTriEnCours IsNot Nothing Then
            If ((ColonneTriEnCours.Tag = Colonne.Tag) And (IconeDeTriEnCours.Direction = NouvelleDirection)) Then Exit Sub
            AdornerLayer.GetAdornerLayer(Colonne).Remove(IconeDeTriEnCours)
            XMLBinding.Items.SortDescriptions.Clear()
        End If
        '           Dim NouvelleDirection As ListSortDirection = ListSortDirection.Ascending
        If ColonneTriEnCours IsNot Nothing Then
            If (ColonneTriEnCours.Tag = Colonne.Tag) And (IconeDeTriEnCours.Direction = ListSortDirection.Ascending) Then
                NouvelleDirection = ListSortDirection.Descending
            End If
        End If
        ColonneTriEnCours = Colonne
        IconeDeTriEnCours = New AdornerSort(ColonneTriEnCours, NouvelleDirection)
        Try
            AdornerLayer.GetAdornerLayer(ColonneTriEnCours).Add(IconeDeTriEnCours)
            XMLBinding.Items.SortDescriptions.Add(New SortDescription(ChampATrier, NouvelleDirection))
            XMLBinding.Items.SortDescriptions.Add(New SortDescription("Artiste", ListSortDirection.Ascending))
            XMLBinding.Items.SortDescriptions.Add(New SortDescription("Titre", ListSortDirection.Ascending))
        Catch ex As Exception
            ColonneTriEnCours = Nothing
        End Try
    End Sub
    'Procedure de rafraichissement du tri
    Public Sub RefreshSort()
        If ColonneTriEnCours IsNot Nothing Then
            XMLBinding.Items.SortDescriptions.Clear()
            XMLBinding.Items.SortDescriptions.Add(New SortDescription(ColonneTriEnCours.Tag, IconeDeTriEnCours.Direction))
            XMLBinding.Items.SortDescriptions.Add(New SortDescription("Artiste", ListSortDirection.Ascending))
        End If
    End Sub


End Class
