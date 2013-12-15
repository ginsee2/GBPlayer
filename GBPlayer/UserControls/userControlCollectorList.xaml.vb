Option Compare Text
Imports System.IO
Imports System.Reflection
Imports System.ComponentModel
Imports System.Xml
Imports System.Windows.Controls.Primitives
Imports System.Text
Imports System.Threading
Imports gbDev.DiscogsServer

'Imports Microsoft.VisualBasic.Devices

Public Class gbListeCollection
    Implements iNotifyCollectionUpdate

    Public Event RequeteRecherche(ByVal ChaineRecherche As String, ByVal NewRequete As Boolean)
    Public Event UpdateLink(ByRef FichierALier As String)
    Public Event RequeteWebBrowser(ByVal url As Uri)
    Public Event RequeteInfosDiscogs(ByVal id As String, ByVal TypeInfos As NavigationDiscogsType)

    Public Event IdCollectionAdded(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate) Implements iNotifyCollectionUpdate.IdCollectionAdded
    Public Event IdCollectionRemoved(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate) Implements iNotifyCollectionUpdate.IdCollectionRemoved
    Public Event IdDiscogsCollectionAdded(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate) Implements iNotifyCollectionUpdate.IdDiscogsCollectionAdded
    Public Event IdDiscogsCollectionRemoved(ByVal id As String, ByVal Transmitter As iNotifyCollectionUpdate) Implements iNotifyCollectionUpdate.IdDiscogsCollectionRemoved

    Dim ListeDesRecherches As List(Of String) = New List(Of String)
    Public Property DisplayValidation As Boolean
    Public ReadOnly Property GetXmlDocument As XmlDocument
        Get
            Return DataProvider.Document
        End Get
    End Property
    Public CollectionChargee As Boolean
    Dim FoldersDataProvider As XmlDataProvider

    Dim MenuContextuel As New ContextMenu
    Private PathFichierCollection As String
    Private Const GBAU_NOMDOSSIER_COLLECTION = "GBDev\GBPlayer\Data"
    Private Const GBAU_NOMFICHIER_COLLECTION = "CollectorList.xml"
    Private Const GBAU_NOMRESSOURCE = "gbDev.DataModelCollectorList.xml"
    Private Const GBAU_VERSIONCOLLECTION = "1.0.12"
    Private Const GBAU_NOMDOSSIER_IMAGESVINYLS = "GBDev\GBPlayer\Images\Vinyls"
    Private Const GBAU_NOMDOSSIER_IMAGESPAYS = "GBDev\GBPlayer\Images\States"

    Private Delegate Sub NoArgDelegate()
    Dim WithEvents TimerDiscogs As Timers.Timer = New Timers.Timer

    '***********************************************************************************************
    '---------------------------------INITIALISATION DE LA CLASSE-----------------------------------
    '***********************************************************************************************
    Public Sub New()
        ' Cet appel est requis par le concepteur.
        InitializeComponent()
        ' Dim appPath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_COLLECTION
        If Not Directory.Exists(RepDest) Then
            Directory.CreateDirectory(RepDest)
        End If
        Try
            PathFichierCollection = RepDest & "\" & GBAU_NOMFICHIER_COLLECTION
            If Not File.Exists(PathFichierCollection) Then
                Dim s As Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GBAU_NOMRESSOURCE)
                Dim b As FileStream = New FileStream(PathFichierCollection, FileMode.Create)
                s.CopyTo(b)
                b.Close()
            End If
            'PARTIE MISE A JOUR DE LA STRUCTURE DU FICHIER
            Dim DocXCollection As XDocument = XDocument.Load(PathFichierCollection)
            If DocXCollection.Root.@Version <> GBAU_VERSIONCOLLECTION Then
                DocXCollection = UpdateFileXml(DocXCollection)
                DocXCollection.Save(PathFichierCollection)
            End If
            DataProvider.Source = New Uri(PathFichierCollection)
            CollectionChargee = True
        Catch ex As Exception
            UploadBackup()
        End Try
    End Sub
    Private Function UpdateFileXml(ByVal FichierExistant As XDocument) As XDocument
        Dim ListeVinylsxmlTemp As XDocument = _
                 <?xml version="1.0" encoding="utf-8"?>
                 <GBPLAYER Version=<%= GBAU_VERSIONCOLLECTION %> Chemin=<%= PathFichierCollection %>>
                     <%= From i In FichierExistant.Root.Elements _
                         Select DuplicateElement(i) %>
                 </GBPLAYER>

        Return ListeVinylsxmlTemp
    End Function
    Private Function DuplicateElement(ByVal Element As XElement) As XElement
        Dim NewElement As New XElement(Element)
        Dim Valeur As XElement =
           <Vinyl>
               <id><%= Element.<id>.Value %></id>
               <instance><%= Element.<instance>.Value %></instance>
               <idFolder><%= Element.<idFolder>.Value %></idFolder>
               <Image><%= Element.<Image>.Value %></Image>
               <Artiste><%= Element.<Artiste>.Value %></Artiste>
               <Titre><%= Element.<Titre>.Value %></Titre>
               <Format><%= Element.<Format>.Value %></Format>
               <Label><%= Element.<Label>.Value %></Label>
               <Catalogue><%= Element.<Catalogue>.Value %></Catalogue>
               <Pays><%= Element.<Pays>.Value %></Pays>
               <Livraison><%= Element.<Livraison>.Value %></Livraison>
               <VinylCollection><%= Element.<VinylCollection>.Value %></VinylCollection>
               <VinylDiscogs><%= Element.<VinylDiscogs>.Value %></VinylDiscogs>
               <VinylWanted><%= Element.<VinylWanted>.Value %></VinylWanted>
               <VinylForSale><%= Element.<VinylForSale>.Value %></VinylForSale>
               <VinylARecevoir><%= Element.<VinylARecevoir>.Value %></VinylARecevoir>
               <VinylAPayer><%= Element.<VinylAPayer>.Value %></VinylAPayer>
               <Annee><%= Element.<Annee>.Value %></Annee>
               <Style><%= Element.<Style>.Value %></Style>
               <Pistes><%= From i In Element.<Pistes>.Elements
                           Select i
                       %></Pistes>
           </Vinyl>
        Return Valeur
    End Function
    Private Sub UploadBackup()
        Dim source As String = Path.GetFileName(PathFichierCollection)
        If Application.Config.collectionList_backupDirectory = "" Then
            Try
                If Directory.Exists(Application.Config.directoriesList_root) Then _
                    FileCopy(Application.Config.directoriesList_root & "\Backup_" & source, PathFichierCollection)
            Catch ex As Exception
            End Try
        Else
            If Directory.Exists(Application.Config.collectionList_backupDirectory) Then _
             FileCopy(Application.Config.collectionList_backupDirectory & "\Backup_" & source, PathFichierCollection)
        End If
        Try
            Dim DocXCollection As XDocument = XDocument.Load(PathFichierCollection)
            If DocXCollection.Root.@Version <> GBAU_VERSIONCOLLECTION Then
                DocXCollection = UpdateFileXml(DocXCollection)
                DocXCollection.Save(PathFichierCollection)
            End If
            DataProvider.Source = New Uri(PathFichierCollection)
            DataProvider.Refresh()
            CollectionChargee = True
        Catch ex As Exception
        End Try
    End Sub

    Private Sub gbListeCollection_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Loaded
        If Not DisplayValidation Then Exit Sub
        '   ActiveTri("Artiste", ListSortDirection.Ascending)
        Dim SauvegardeCollection As New Dictionary(Of String, GridViewColumn)
        For Each i As GridViewColumn In CType(XMLBinding.View, GridView).Columns
            SauvegardeCollection.Add(CType(i.Header, GridViewColumnHeader).Content.ToString, i)
        Next
        CType(XMLBinding.View, GridView).Columns.Clear()
        Application.Config.collectionList_columns.ForEach(Sub(c As ConfigApp.ColumnDescription)
                                                              Try
                                                                  Dim NomColonne As String = c.Name
                                                                  Dim Dimension As Double = c.Size
                                                                  Dim Colonne As GridViewColumn = SauvegardeCollection.Item(NomColonne)
                                                                  SauvegardeCollection.Remove(NomColonne)
                                                                  Colonne.Width = Dimension
                                                                  CType(XMLBinding.View, GridView).Columns.Add(Colonne)
                                                              Catch ex As Exception
                                                              End Try
                                                          End Sub)
        If SauvegardeCollection.Count > 0 Then
            For Each i In SauvegardeCollection
                CType(XMLBinding.View, GridView).Columns.Add(i.Value)
            Next
        End If
        ActiveTri(Application.Config.collectionList_sortColumn.Name, Application.Config.collectionList_sortColumn.SortDirection)
        If System.Environment.OSVersion.Platform = PlatformID.Win32NT Then
            If System.Environment.OSVersion.Version.Major > 5 Then PlateformVista = True
        End If
        NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
        Me.AddHandler(ContextMenuOpeningEvent, New ContextMenuEventHandler(AddressOf MenuContextuel_Opened))
        Me.AddHandler(ContextMenuClosingEvent, New ContextMenuEventHandler(AddressOf MenuContextuel_Closed))
        'Dim myView As CollectionView = CType(CollectionViewSource.GetDefaultView(XMLBinding.ItemsSource), CollectionView)
        'If myView.CanGroup = True Then
        'Dim groupDescription As New PropertyGroupDescription("Artiste")
        'myView.GroupDescriptions.Add(groupDescription)
        'Else : Return
        'End If
    End Sub
    Private Sub gbListeCollection_Unloaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Unloaded
        If Not DisplayValidation Then Exit Sub
        SaveConfiguration()
    End Sub
    Public Sub SaveConfiguration()
        If Not DisplayValidation Then Exit Sub
        ConfigApp.UpdateListeColonnes(Application.Config.collectionList_columns, CType(XMLBinding.View, GridView).Columns)
        If ColonneTriEnCours IsNot Nothing Then
            Application.Config.collectionList_sortColumn = New ConfigApp.DescriptionTri(ColonneTriEnCours.Tag,
                                    IconeDeTriEnCours.Direction)
        End If
        Dim source As String = DataProvider.Source.LocalPath
        DataProvider.Document.Save(source)
    End Sub
    Private Sub gbListeCollection_Initialized(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Initialized
        FoldersDataProvider = CType(FindResource("FoldersDataProvider"), XmlDataProvider)
        FoldersDataProvider.Source = New Uri(DiscogsServer.FilePathCollectionFolders(Application.Config.user_name))
        'StyleDataProvider = CType(FindResource("StyleDataProvider"), XmlDataProvider)
        'StyleDataProvider.Source = tagID3FilesInfosDO.GetDataProvider
    End Sub

    '***********************************************************************************************
    '---------------------------------DESTRUCTION DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Protected Overrides Sub Finalize()
        If CollectionChargee Then
            DataProvider.Document.Save(PathFichierCollection)
        End If
        MyBase.Finalize()
    End Sub

    '***********************************************************************************************
    '------------------------------ACTION DE MISE A JOUR COLLECTION---------------------------------
    '***********************************************************************************************
    Public Sub UpdateIdCollection(ByVal idRelease As String)
        Dim NodeSelectionne As XmlElement = CType(XMLBinding.SelectedItem, XmlElement)
        Select Case XMLBinding.SelectedItems.Count
            Case 1
                If wpfMsgBox.MsgBoxQuestion("Mise a jour collection", "Voulez-vous remplacer l'ID du vinyl sélectionné?", Nothing, NodeSelectionne.SelectSingleNode("Artiste").InnerText & _
                                            " - " & NodeSelectionne.SelectSingleNode("Titre").InnerText, "Remplacer", "Ajouter") Then
                    Dim idASupprimer As String = NodeSelectionne.SelectSingleNode("id").InnerText
                    NodeSelectionne.SelectSingleNode("id").InnerText = idRelease
                    RecuperationInfos(NodeSelectionne)
                    RaiseEvent IdCollectionAdded(idRelease, Me)
                    If Not NodeExist(idASupprimer) Then
                        If NodeSelectionne.SelectSingleNode("VinylDiscogs").InnerText = True Then
                            DiscogsServer.RequestDelete_CollectionId(Application.Config.user_name, idASupprimer,
                                                               New DelegateRequestResult(AddressOf DiscogsServerDeleteIdResultNotify))
                        End If
                        RaiseEvent IdCollectionRemoved(idASupprimer, Me)
                    End If
                    NodeSelectionne.SelectSingleNode("VinylCollection").InnerText = True
                Else
                    CreateIdCollection(, idRelease)
                End If
            Case Else
                If wpfMsgBox.MsgBoxQuestion("Ajouter un vinyl", "Voulez-vous ajouter le vinyl avec le numéro d'ID : " & idRelease, Nothing) Then
                    CreateIdCollection(, idRelease)
                End If
        End Select
    End Sub

    Private Sub CreateIdCollection(Optional ByVal Mp3FilesInfos As tagID3FilesInfosDO = Nothing, Optional ByVal newIDRelease As String = "")
        Dim IDRepere As String = ""
        Dim doc As New XmlDocument()
        doc.LoadXml("<Vinyl>" & _
                        "<id>-1</id>" & _
                        "<instance>0</instance>" & _
                        "<idFolder></idFolder>" & _
                        "<Image></Image>" & _
                        "<Artiste>_Nouvel Artiste</Artiste>" & _
                        "<Titre></Titre>" & _
                        "<Format></Format>" & _
                        "<Label></Label>" & _
                        "<Catalogue></Catalogue>" & _
                        "<Pays></Pays>" & _
                        "<Livraison></Livraison>" & _
                        "<VinylCollection>False</VinylCollection>" & _
                        "<VinylDiscogs>False</VinylDiscogs>" & _
                        "<VinylWanted>False</VinylWanted>" & _
                        "<VinylForSale>False</VinylForSale>" & _
                        "<VinylARecevoir>False</VinylARecevoir>" & _
                        "<VinylAPayer>False</VinylAPayer>" & _
                        "<Annee></Annee>" & _
                        "<Style></Style>" & _
                        "<Pistes></Pistes>" & _
                    "</Vinyl>")
        Dim NouveauVinyl As XmlElement = doc.DocumentElement
        If (Mp3FilesInfos Is Nothing) And newIDRelease = "" Then
            IDRepere = -1
        Else
            ' NouveauVinyl.SelectSingleNode("Artiste").InnerText = Mp3FilesInfos.Artiste
            ' NouveauVinyl.SelectSingleNode("Titre").InnerText = Mp3FilesInfos.Titre
            If newIDRelease <> "" Then IDRepere = newIDRelease Else IDRepere = Mp3FilesInfos.idRelease
            NouveauVinyl.SelectSingleNode("id").InnerText = IDRepere
            RecuperationInfos(NouveauVinyl)
            NouveauVinyl.SelectSingleNode("VinylCollection").InnerText = True
            RaiseEvent IdCollectionAdded(IDRepere, Me)
            ' NouveauVinyl.SelectSingleNode("VinylARecevoir").InnerText = True
            ' NouveauVinyl.SelectSingleNode("VinylAPayer").InnerText = True
        End If
        Dim newBook As XmlNode = DataProvider.Document.ImportNode(NouveauVinyl, True)
        DataProvider.Document.SelectSingleNode("GBPLAYER").AppendChild(newBook)
        RefreshSort()
        XMLBinding.SelectedItem = newBook
        XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
        NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
    End Sub
    Private Sub DeleteIdCollection()
        Dim NodeSelectionne As XmlElement = CType(XMLBinding.SelectedItem, XmlElement)
        If wpfMsgBox.MsgBoxQuestion("Confirmation suppression",
                                    IIf(XMLBinding.SelectedItems.Count > 1, "Voulez-vous supprimer les vinyls sélectionnés?", _
                                                                            "Voulez-vous supprimer le vinyl sélectionné?"), Me, _
                                    IIf(XMLBinding.SelectedItems.Count > 1, XMLBinding.SelectedItems.Count & " vinyls sélectionnés", _
                                        NodeSelectionne.SelectSingleNode("Artiste").InnerText & _
                                        " - " & NodeSelectionne.SelectSingleNode("Titre").InnerText)) Then
            Dim XMLBindingAEffacer As New List(Of XmlElement)
            Dim NodeASelectionner As XmlElement = Nothing
            For Each i As XmlElement In XMLBinding.SelectedItems
                XMLBindingAEffacer.Add(i)
            Next
            XMLBindingAEffacer.ForEach(Sub(ElementASupprimer As XmlElement)
                                           NodeASelectionner = ElementASupprimer.NextSibling
                                           ElementASupprimer.ParentNode().RemoveChild(ElementASupprimer)
                                           If Not NodeExist(ElementASupprimer.SelectSingleNode("id").InnerText) Then
                                               If ElementASupprimer.SelectSingleNode("VinylDiscogs").InnerText = True Then
                                                   DiscogsServer.RequestDelete_CollectionId(Application.Config.user_name, ElementASupprimer.SelectSingleNode("id").InnerText,
                                                                                      New DelegateRequestResult(AddressOf DiscogsServerDeleteIdResultNotify))
                                               End If
                                               RaiseEvent IdCollectionRemoved(ElementASupprimer.SelectSingleNode("id").InnerText, Me)
                                           End If
                                       End Sub)
            XMLBinding.SelectedItem = NodeASelectionner
            RefreshSort()
            NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
        End If
    End Sub

    Private Function NodeExist(ByVal id As String) As Boolean
        For Each i As XmlElement In DataProvider.Document.DocumentElement.ChildNodes
            If i.SelectSingleNode("id").InnerText = id Then
                Return True
                Exit For
            End If
        Next
        Return False
    End Function

    '***********************************************************************************************
    '---------------------------------GESTION ELEMENTS ENTETE DE LA LISTE DES VINYLS----------------
    '***********************************************************************************************
    Private Sub BPUpdateCollection_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles BPUpdateCollection.Click
        Me.IsEnabled = False
        NbreElementsAffiches.Text = "Mise a jour collection en cours...."
        DiscogsServer.RequestGet_CollectionListAll(Application.Config.user_name, New DelegateRequestResult(AddressOf DiscogsServerGetAllResultNotify))
        DiscogsServer.RequestGet_CollectionFolderListAll(Application.Config.user_name, New DelegateRequestResult(AddressOf DiscogsServerGetFolderResultNotify))
    End Sub
    Private Sub DataProvider_DataChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles DataProvider.DataChanged
        NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
    End Sub
    Private Sub BPEnregistrer_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        If CollectionChargee Then
            Dim source As String = Path.GetFileName(DataProvider.Source.LocalPath)
            Try
                If Application.Config.collectionList_backupDirectory = "" Then
                    If Directory.Exists(Application.Config.directoriesList_root) Then _
                        DataProvider.Document.Save(Application.Config.directoriesList_root & "\Backup_" & source)
                Else
                    If Directory.Exists(Application.Config.collectionList_backupDirectory) Then _
                        DataProvider.Document.Save(Application.Config.collectionList_backupDirectory & "\Backup_" & source)
                End If
            Catch ex As Exception
            End Try
            DataProvider.Document.Save(PathFichierCollection)
        End If
    End Sub
    Private Sub BPUploadBackup_Click_1(sender As Object, e As RoutedEventArgs)
        If wpfMsgBox.MsgBoxQuestion("Chargement de la sauvegarde", "Voulez vous charger la sauvegarde de la collection?") Then
            UploadBackup()
        End If
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DES FOLDERS DANS LA LISTE DES VINYLS------------------
    '***********************************************************************************************
    Private Sub BoutonsAjouterSupprimerFolders_Click(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles CollectionFolderList.PreviewMouseLeftButtonDown
        Dim Bordure As Border = wpfApplication.FindAncestor(e.OriginalSource, "Border")
        If Bordure IsNot Nothing Then
            Dim Bouton As Button = wpfApplication.FindAncestor(e.OriginalSource, "Button")
            If Bouton IsNot Nothing Then
                If Bouton.Name = "BoutonAjouter" Then
                    Dim ItemSurvole As ComboBoxItem = wpfApplication.FindAncestor(Bouton, "ComboBoxItem") ' CType(CollectionFolderList.ContainerFromElement(e.OriginalSource), ComboBoxItem)
                    Dim list As ComboBox = ItemsControl.ItemsControlFromItemContainer(ItemSurvole)
                    Dim NomNouveauRepertoire As String = wpfMsgBox.InputBox("Nom du répertoire à ajouter", list, "Entrer un nom de répertoire non existant dans Discogs")
                    If wpfMsgBox.EtatDialogue Then
                        For Each i As XmlElement In FoldersDataProvider.Document.DocumentElement.ChildNodes
                            If i.SelectSingleNode("name").InnerText = NomNouveauRepertoire Then
                                Exit Sub
                            End If
                        Next
                        NbreElementsAffiches.Text = "Création répertoire...."
                        DiscogsServer.RequestAdd_CollectionFolder(Application.Config.user_name, NomNouveauRepertoire,
                                                            New DelegateRequestResult(AddressOf DiscogsServerAddFolderResultNotify))
                    End If
                End If
                If Bouton.Name = "BoutonSupprimer" Then
                    Dim ItemSurvole As ComboBoxItem = wpfApplication.FindAncestor(Bouton, "ComboBoxItem") ' CType(.ContainerFromElement(e.OriginalSource), ComboBoxItem)
                    Dim list As ComboBox = ItemsControl.ItemsControlFromItemContainer(ItemSurvole)
                    Dim DonneeSurvolee As XmlElement = CType(ItemSurvole.Content, XmlElement)
                    For Each i As XmlElement In FoldersDataProvider.Document.DocumentElement.ChildNodes
                        If i.SelectSingleNode("name").InnerText = DonneeSurvolee.InnerText Then
                            If CInt(i.SelectSingleNode("id").InnerText) > 1 Then
                                For Each j As XmlElement In FoldersDataProvider.Document.DocumentElement.ChildNodes
                                    If j.SelectSingleNode("id").InnerText = i.SelectSingleNode("id").InnerText Then
                                        FoldersDataProvider.Document.DocumentElement.RemoveChild(j)
                                        Exit For
                                    End If
                                Next
                                NbreElementsAffiches.Text = "Suppression répertoire...."
                                DiscogsServer.RequestDelete_CollectionFolder(Application.Config.user_name, i.OuterXml,
                                                                    New DelegateRequestResult(AddressOf DiscogsServerDeleteFolderResultNotify))
                                Exit Sub
                            End If
                        End If
                    Next
                End If
            End If
        End If
    End Sub
    Dim LockUpdateFolderSearch As Boolean
    Private Sub CollectionFolderList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles CollectionFolderList.SelectionChanged
        If Not LockUpdateFolderSearch Then
            If e.AddedItems.Count > 0 Then
                If TempLockLink Then Exit Sub
                Dim NodeSelectionne As XmlElement = CType(XMLBinding.SelectedItem, XmlElement)
                Dim Selection As String = ""
                If NodeSelectionne IsNot Nothing Then
                    If NodeSelectionne.SelectSingleNode("id").InnerText <> "" Then
                        Selection = "id:" & NodeSelectionne.SelectSingleNode("id").InnerText
                    Else
                        Selection = "artiste:" & NodeSelectionne.SelectSingleNode("Artiste").InnerText
                    End If
                End If
                ListeDesRecherches.Add("FolderPre:" & CollectionFolderList.Text & "\\{" & Selection)
                RecherchePrecedente.IsEnabled = True
                UpdateFiltre(CType(e.AddedItems.Item(0), XmlElement).InnerText)
            End If
        End If
    End Sub
    Private Sub CollectionFolderList2_SourceUpdated(ByVal sender As System.Object, ByVal e As System.Windows.Data.DataTransferEventArgs)
        Dim list As ComboBox = e.OriginalSource 'CType(e.TargetObject, ComboBox) ' ItemsControl.ItemsControlFromItemContainer(ItemSurvole)
        Dim idFolderSource As String = 0
        Dim idFolderDestination As String = 0
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            Dim ItemSurvole As ListViewItem = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            Dim DonneeSurvolee As XmlElement = CType(ItemSurvole.Content, XmlElement)
            If list.SelectedItem IsNot Nothing Then
                Dim NouvelleValeur As String = CType(list.SelectedItem, XmlElement).InnerText
                For Each j As XmlElement In FoldersDataProvider.Document.DocumentElement.ChildNodes
                    If j.SelectSingleNode("name").InnerText = Trim(list.Text) Then
                        idFolderSource = j.SelectSingleNode("id").InnerText
                    ElseIf j.SelectSingleNode("name").InnerText = NouvelleValeur Then
                        idFolderDestination = j.SelectSingleNode("id").InnerText
                    End If
                Next
                If (idFolderDestination > 0) Then
                    If idFolderSource = 0 Then idFolderSource = 1
                    If DonneeSurvolee.SelectSingleNode("instance").InnerText = "" Then DonneeSurvolee.SelectSingleNode("instance").InnerText = CStr(1)
                    DiscogsServer.RequestMove_CollectionId(Application.Config.user_name, DonneeSurvolee.SelectSingleNode("id").InnerText,
                                                       DonneeSurvolee.SelectSingleNode("instance").InnerText, idFolderSource,
                                                       idFolderDestination, AddressOf DiscogsServerMoveIdResultNotify)
                Else
                    DiscogsServerMoveIdResultNotify(DonneeSurvolee.SelectSingleNode("id").InnerText & "/" &
                                                    DonneeSurvolee.SelectSingleNode("instance").InnerText, idFolderSource)
                End If
                'Else
                '    list.SelectedValue = DonneeSurvolee.SelectSingleNode("idFolder").InnerText
            End If
        End If
    End Sub

    '***********************************************************************************************
    '---------------GESTION DES NOTIFICATIONS DE MISE A JOUR COLLECTION ET WANTLIST-----------------
    '***********************************************************************************************
    Public Function NotifyAddIdCollection(ByVal Id As String) As Boolean Implements iNotifyCollectionUpdate.NotifyAddIdCollection
    End Function
    Public Function NotifyRemoveIdCollection(ByVal id As String) As Boolean Implements iNotifyCollectionUpdate.NotifyRemoveIdCollection
    End Function
    Public Function NotifyAddIdDiscogsCollection(ByVal Id As String) As Boolean Implements iNotifyCollectionUpdate.NotifyAddIdDiscogsCollection
        Dim NodeAAjouter As XmlElement = Nothing
        Dim ViewCollection As ICollectionView = CollectionViewSource.GetDefaultView(XMLBinding.Items)
        Dim MemNumInstance As Integer = 0
        For Each i As XmlElement In DataProvider.Document.DocumentElement.ChildNodes
            If i.SelectSingleNode("id").InnerText = Id Then
                NodeAAjouter = i
                XMLBinding.ScrollIntoView(NodeAAjouter)
                Exit For
            End If
        Next
        If NodeAAjouter IsNot Nothing Then
            XMLBinding.SelectedItem = NodeAAjouter
            NodeAAjouter.SelectSingleNode("VinylDiscogs").InnerText = True
            NodeAAjouter.SelectSingleNode("instance").InnerText = CStr(1)
        Else
            CreateIdCollection(, Id)
            If XMLBinding.SelectedItem IsNot Nothing Then
                CType(XMLBinding.SelectedItem, XmlElement).SelectSingleNode("VinylDiscogs").InnerText = True
                CType(XMLBinding.SelectedItem, XmlElement).SelectSingleNode("instance").InnerText = CStr(1)
            End If
        End If
        Return True
    End Function
    Public Function NotifyRemoveIdDiscogsCollection(ByVal id As String) As Boolean Implements iNotifyCollectionUpdate.NotifyRemoveIdDiscogsCollection
        For Each i As XmlElement In DataProvider.Document.DocumentElement.ChildNodes
            If i.SelectSingleNode("id").InnerText = id Then
                XMLBinding.SelectedItem = i
                i.SelectSingleNode("VinylDiscogs").InnerText = False
                Exit For
            End If
        Next
        Return True
    End Function

    '***********************************************************************************************
    '------------------------------DELEGATE POUR REPONSE ACTIONS DISCOGSSERVER----------------------
    '***********************************************************************************************
    Private Sub DiscogsServerGetFolderResultNotify(ByVal xmlFileResult As String, ByVal IdRelease As String)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           If xmlFileResult <> "" Then
                                                               FoldersDataProvider.Source = Nothing
                                                               FoldersDataProvider.Source = New Uri(xmlFileResult)
                                                               FoldersDataProvider.Refresh()
                                                           End If
                                                           Me.IsEnabled = True
                                                           NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
                                                       End Sub))
    End Sub
    Private Sub DiscogsServerAddFolderResultNotify(ByVal AddResult As String, ByVal FolderName As String)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           If AddResult <> "" Then
                                                               Dim FichierxmlFinal As XDocument = _
                                                                        <?xml version="1.0" encoding="utf-8"?>
                                                                        <COLLECTIONFOLDERLIST>
                                                                        </COLLECTIONFOLDERLIST>
                                                               If FoldersDataProvider.Source IsNot Nothing Then
                                                                   Dim PathFile As String = FoldersDataProvider.Source.LocalPath
                                                                   Dim Element As XElement = XElement.Parse(AddResult)
                                                                   Dim Doc As XDocument = XDocument.Parse(FoldersDataProvider.Document.InnerXml)
                                                                   Doc.Root.Add(Element)
                                                                   Dim RequeteFolders = (From rel In Doc.<COLLECTIONFOLDERLIST>.<folders>
                                                                                          Select rel Order By rel.<name>.Value)
                                                                   FichierxmlFinal.Root.Add(RequeteFolders)
                                                                   FichierxmlFinal.Save(PathFile)
                                                                   FoldersDataProvider.Source = New Uri(PathFile)
                                                                   FoldersDataProvider.Refresh()

                                                               End If
                                                           End If
                                                           NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
                                                       End Sub))
    End Sub
    Private Sub DiscogsServerDeleteFolderResultNotify(ByVal DeleteResult As String, ByVal Folderid As String)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           If DeleteResult = "" Then
                                                               Dim PathFile As String = FoldersDataProvider.Source.LocalPath
                                                               FoldersDataProvider.Document.Save(PathFile)
                                                           Else
                                                               DiscogsServerAddFolderResultNotify(DeleteResult, "")
                                                               If Folderid <> "" Then
                                                                   wpfMsgBox.MsgBoxInfo("Echec de la suppression du folder", "Le folder doit être vide avant d'être supprimé.", Me, "Le folder contiend " & Folderid & " releases")
                                                               End If
                                                           End If
                                                           NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
                                                       End Sub))
    End Sub

    Private Sub DiscogsServerGetAllResultNotify(ByVal xmlFileResult As String, ByVal IdRelease As String)
        Dim DocXCollection As XDocument = Nothing
        If xmlFileResult <> "" Then
            DataProvider.Document.Save(PathFichierCollection)
            DocXCollection = XDocument.Load(PathFichierCollection)
            For Each Disque In From i In DocXCollection.<GBPLAYER>.<Vinyl> _
                            Where i.<VinylDiscogs>.Value = CStr(True) _
                            Select i
                Disque.<VinylDiscogs>.Value = CStr(-1)
                Disque.<idFolder>.Value = ""
            Next
            Dim DocXDiscogs As XDocument = XDocument.Load(xmlFileResult)
            Dim DisquePrecedent As XElement = Nothing
            For Each Disque In From i In DocXCollection.<GBPLAYER>.<Vinyl> _
                            Join j In DocXDiscogs.<COLLECTIONLIST>.<releases> _
                            On i.<id>.Value Equals j.<basic_information>.<id>.Value _
                            Select j, i Order By i.<id>.Value

                Disque.i.<idFolder>.Value = Disque.j.<folder_id>.Value
                Disque.i.<instance>.Value = Disque.j.<instance_id>.Value
                Disque.j.<instance_id>.Value = CStr(-1)
                If DisquePrecedent IsNot Nothing Then
                    If DisquePrecedent.<id>.Value = Disque.i.<id>.Value Then
                        If Disque.i.<VinylDiscogs>.Value = CStr(-1) Then
                            Disque.i.<VinylDiscogs>.Value = True
                            'Disque.<VinylAPayer>.Value = True
                            'DisquePrecedent.<VinylAPayer>.Value = True
                            DisquePrecedent.<VinylDiscogs>.Value = False
                            DisquePrecedent = Disque.i
                        End If
                    Else
                        Disque.i.<VinylDiscogs>.Value = True
                        DisquePrecedent = Disque.i
                    End If
                Else
                    Disque.i.<VinylDiscogs>.Value = True
                    DisquePrecedent = Disque.i
                End If
            Next
            For Each Disque In From i In DocXCollection.<GBPLAYER>.<Vinyl> _
                            Where i.<VinylDiscogs>.Value = CStr(-1) _
                            Select i
                Disque.<VinylDiscogs>.Value = CStr(False)
            Next
            For Each Disque In From j In DocXDiscogs.<COLLECTIONLIST>.<releases> _
                            Where j.<instance_id>.Value <> CStr(-1) _
                            Select j

                Dim NewElement As XElement = <Vinyl>
                                                 <id><%= Disque.<id>.Value %></id>
                                                 <instance><%= Disque.<instance_id>.Value %></instance>
                                                 <idFolder><%= Disque.<folder_id>.Value %></idFolder>
                                                 <Image><%= Disque.<id>.Value %></Image>
                                                 <Artiste><%= Disque.<basic_information>.<artists>.<anv>.Value %></Artiste>
                                                 <Titre><%= Disque.<basic_information>.<title>.Value %></Titre>
                                                 <Format><%= Disque.<basic_information>.<formats>.<descriptions>.Value %></Format>
                                                 <Label><%= Disque.<basic_information>.<labels>.<name>.Value %></Label>
                                                 <Catalogue><%= Disque.<basic_information>.<labels>.<catno>.Value %></Catalogue>
                                                 <Pays>Achercher</Pays>
                                                 <Livraison></Livraison>
                                                 <VinylCollection>True</VinylCollection>
                                                 <VinylDiscogs>True</VinylDiscogs>
                                                 <VinylWanted>False</VinylWanted>
                                                 <VinylForSale>False</VinylForSale>
                                                 <VinylARecevoir>False</VinylARecevoir>
                                                 <VinylAPayer>False</VinylAPayer>
                                                 <Annee><%= Disque.<basic_information>.<year>.Value %></Annee>
                                                 <Style></Style>
                                                 <Pistes></Pistes>
                                             </Vinyl>
                DocXCollection.Root.Add(NewElement)
                Disque.<instance_id>.Value = CStr(False)
            Next
            Dim DocXDiscogsFolders As XDocument = XDocument.Load(DiscogsServer.FilePathCollectionFolders(Application.Config.user_name))
            If DocXCollection IsNot Nothing Then
                For Each Disque In From i In DocXCollection.<GBPLAYER>.<Vinyl> _
                                   Join j In DocXDiscogsFolders.<COLLECTIONFOLDERLIST>.<folders> _
                                   On i.<idFolder>.Value Equals j.<id>.Value
                                   Select i, j

                    If Disque.i.<VinylDiscogs>.Value = CStr(True) Then
                        Disque.i.<idFolder>.Value = Disque.j.<name>.Value
                    Else
                        Disque.i.<idFolder>.Value = ""
                    End If
                Next
            End If
        End If
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           If xmlFileResult <> "" Then
                                                               DataProvider.Source = Nothing
                                                               DocXCollection.Save(PathFichierCollection)
                                                               DataProvider.Source = New Uri(PathFichierCollection)
                                                               DataProvider.Refresh()
                                                           Else
                                                               wpfMsgBox.MsgBoxInfo("La mise à jour à échouée", "Les informations Discogs n'ont pas pu être mis à jour", Me)
                                                           End If
                                                           Me.IsEnabled = True
                                                           NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
                                                       End Sub))
    End Sub
    Private Sub DiscogsServerAddIdResultNotify(ByVal AddResult As String, ByVal IdRelease As String)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           If AddResult <> "" Then
                                                               Dim Resultat As XDocument = XDocument.Parse(AddResult)
                                                               For Each i As XmlElement In DataProvider.Document.DocumentElement.ChildNodes
                                                                   If i.SelectSingleNode("id").InnerText = IdRelease Then
                                                                       If i.SelectSingleNode("instance").InnerText = "0" Then
                                                                           i.SelectSingleNode("VinylDiscogs").InnerText = True
                                                                           i.SelectSingleNode("instance").InnerText = Resultat.<releases>.<instance_id>.Value
                                                                           Exit For
                                                                       End If
                                                                   End If
                                                               Next
                                                               RaiseEvent IdDiscogsCollectionAdded(IdRelease, Me)
                                                           Else
                                                               For Each i As XmlElement In DataProvider.Document.DocumentElement.ChildNodes
                                                                   If i.SelectSingleNode("id").InnerText = IdRelease Then
                                                                       XMLBinding.SelectedItem = i
                                                                       i.SelectSingleNode("VinylDiscogs").InnerText = False
                                                                       Exit For
                                                                   End If
                                                               Next
                                                           End If
                                                       End Sub))
    End Sub
    Private Sub DiscogsServerDeleteIdResultNotify(ByVal DeleteResult As String, ByVal IdRelease As String)
        Dim Retour As Boolean = CBool(DeleteResult)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           If Retour Then
                                                               For Each i As XmlElement In DataProvider.Document.DocumentElement.ChildNodes
                                                                   If i.SelectSingleNode("id").InnerText = IdRelease Then
                                                                       i.SelectSingleNode("instance").InnerText = "0"
                                                                       i.SelectSingleNode("idFolder").InnerText = ""
                                                                       Exit For
                                                                   End If
                                                               Next
                                                               RaiseEvent IdDiscogsCollectionRemoved(IdRelease, Me)
                                                           Else
                                                               For Each i As XmlElement In DataProvider.Document.DocumentElement.ChildNodes
                                                                   If i.SelectSingleNode("id").InnerText = IdRelease Then
                                                                       XMLBinding.SelectedItem = i
                                                                       i.SelectSingleNode("VinylDiscogs").InnerText = True
                                                                       Exit For
                                                                   End If
                                                               Next
                                                           End If
                                                       End Sub))

    End Sub
    Private Sub DiscogsServerMoveIdResultNotify(ByVal MoveResult As String, ByVal IdFolder As String)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           'If MoveResult <> "" Then
                                                           Dim Result As String() = Split(MoveResult, "/")
                                                           Dim id As String = Result(0)
                                                           Dim inst As String = Result(1)
                                                           Dim FolderName As String = ""
                                                           Dim j As XmlDocument = DataProvider.Document
                                                           For Each i As XmlElement In FoldersDataProvider.Document.DocumentElement.ChildNodes
                                                               If i.SelectSingleNode("id").InnerText = IdFolder Then
                                                                   FolderName = i.SelectSingleNode("name").InnerText
                                                                   Exit For
                                                               End If
                                                           Next
                                                           If FolderName <> "" Then
                                                               Dim Elements = DataProvider.Document.SelectSingleNode("descendant::Vinyl" & _
                                                                                                        "[id='" & id & "' and instance='" & inst & "']")
                                                               If Elements IsNot Nothing Then
                                                                   Elements.SelectSingleNode("idFolder").InnerText = FolderName ' & " "
                                                               End If
                                                           End If
                                                           'End If
                                                       End Sub))
    End Sub


    '***********************************************************************************************
    '---------------------------------GESTION DES MENUS LA LISTE DES VINYLS-------------------------
    '***********************************************************************************************
    Private Sub XmlBinding_PreviewMouseRightButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles XMLBinding.PreviewMouseRightButtonDown
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As XmlElement = Nothing
        XMLBinding.ContextMenu = Nothing
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            DonneeSurvolee = CType(ItemSurvole.Content, XmlElement)
        End If
        If TypeOf (e.OriginalSource) Is Border Then
            If CType(e.OriginalSource, Border).Name = "DragDropBorder" Then
                If ItemSurvole.IsSelected Then
                    XMLBinding.ContextMenu = CreationMenuContextuelDynamique("General")
                End If
            End If
            e.Handled = True
        End If
        If TypeOf (e.OriginalSource) Is Image Then
            If CType(e.OriginalSource, Image).Name = "Drapeau" Then
                If ItemSurvole.IsSelected Then
                    XMLBinding.ContextMenu = CreationMenuContextuelDynamique("Drapeau")
                End If
            End If
            e.Handled = True
        End If
    End Sub
    Private Function CreationMenuContextuelDynamique(ByVal NomChamp As String) As ContextMenu
        Dim DataConfig As TagID3Data = gbDev.TagID3Data.LoadConfig()
        If MenuContextuel IsNot Nothing Then
            If MenuContextuel.Tag = NomChamp Then Return MenuContextuel
        End If
        MenuContextuel = New ContextMenu
        MenuContextuel.Tag = NomChamp
        Select Case NomChamp
            Case "General"
                Dim ListeMenu As New List(Of String) 'Libelle menu;Tag envoyé à la fonction de reponse,Nom sous menu
                ListeMenu.Add("Supprimer un vinyl;SupprimerVinyl;;supprimervinyl24.png")
                ListeMenu.Add(";;;")
                ListeMenu.Add("Modifier le pays;;Drapeau;modifierpays.png")
                ListeMenu.ForEach(Sub(i As String)
                                      Dim ItemMenu As New MenuItem
                                      Dim TabChaine() As String = Split(i, ";")
                                      If TabChaine(0) <> "" Then
                                          If TabChaine(1) <> "" Then
                                              ItemMenu.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf MenuDynamique_Click))
                                              ItemMenu.Name = TabChaine(1)
                                              ItemMenu.Tag = TabChaine(1)
                                          End If
                                          If TabChaine(2) <> "" Then CreationItemsDynamiques(TabChaine(2), ItemMenu.Items)
                                          If TabChaine.Count >= 4 Then
                                              If TabChaine(3) <> "" Then
                                                  Dim ImageIcon As Image = New Image()
                                                  ImageIcon.Height = 16
                                                  ImageIcon.Width = 16
                                                  ImageIcon.Stretch = Stretch.Fill
                                                  ImageIcon.Source = GetBitmapImage("../Images/imgmenus/" & TabChaine(3))
                                                  ItemMenu.Icon = ImageIcon
                                              End If
                                          End If
                                          ItemMenu.Header = TabChaine(0)
                                          MenuContextuel.Items.Add(ItemMenu)
                                      Else
                                          MenuContextuel.Items.Add(New Separator)
                                      End If
                                  End Sub)
            Case "Drapeau"
                CreationItemsDynamiques("Drapeau", MenuContextuel.Items)
        End Select
        Return MenuContextuel
    End Function
    Private Sub CreationItemsDynamiques(ByVal NomChamp As String, ByVal ItemsMenu As ItemCollection)
        Dim DataConfig As TagID3Data = gbDev.TagID3Data.LoadConfig()
        Dim Converter As ConverterStringToFlagPath = New ConverterStringToFlagPath
        Select Case NomChamp
            Case "Drapeau"
                Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_IMAGESPAYS
                If Not Directory.Exists(RepDest) Then
                    Directory.CreateDirectory(RepDest)
                End If
                Dim ItemMenu As New MenuItem
                Dim ImageIcon As Image = New Image()
                ImageIcon.Height = 16
                ImageIcon.Width = 16
                ImageIcon.Stretch = Stretch.Fill
                ImageIcon.Source = GetBitmapImage("../Images/imgmenus/effacer24.png")
                ItemMenu.Icon = ImageIcon
                ItemMenu.Header = "Effacer..."
                ItemMenu.Tag = ""
                ItemMenu.Name = "Drapeau"
                ItemMenu.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf MenuDynamique_Click))
                ItemsMenu.Add(ItemMenu)
                ItemsMenu.Add(New Separator)
                Array.ForEach(Directory.GetFiles(RepDest), Sub(i As String)
                                                               ItemMenu = New MenuItem
                                                               If File.Exists(i) Then
                                                                   Dim ImageDrapeau As Image = New Image()
                                                                   Dim BiDrapeau As BitmapImage = New BitmapImage()
                                                                   Dim NomImage As String = i
                                                                   Try
                                                                       If NomImage IsNot Nothing Then
                                                                           BiDrapeau.BeginInit()
                                                                           BiDrapeau.UriSource = New Uri(NomImage)
                                                                           BiDrapeau.EndInit()
                                                                           ImageDrapeau.Height = 16
                                                                           ImageDrapeau.Width = 20
                                                                           ImageDrapeau.Stretch = Stretch.Fill
                                                                           ImageDrapeau.Source = BiDrapeau
                                                                           ItemMenu.Icon = ImageDrapeau
                                                                       End If
                                                                       ItemMenu.Header = Path.GetFileNameWithoutExtension(i)
                                                                       ItemMenu.Tag = Path.GetFileNameWithoutExtension(i)
                                                                       ItemMenu.Name = "Drapeau"
                                                                       ItemMenu.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf MenuDynamique_Click))
                                                                       ItemsMenu.Add(ItemMenu)
                                                                   Catch ex As Exception
                                                                   End Try
                                                               Else
                                                                   ItemsMenu.Add(New Separator)
                                                               End If
                                                           End Sub)
        End Select
        Return
    End Sub

    Sub MenuDynamique_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
        Select Case CType(e.OriginalSource, MenuItem).Name
            Case "SupprimerVinyl"
                DeleteIdCollection()
            Case "Drapeau"
                For Each i As XmlElement In XMLBinding.SelectedItems
                    For Each j As XmlElement In i.ChildNodes
                        If j.Name = "Pays" Then
                            j.InnerText = CType(e.OriginalSource, MenuItem).Tag
                            Exit For
                        End If
                    Next
                Next
        End Select
    End Sub
    Private Function GetBitmapImage(ByVal NomImage As String) As BitmapImage
        Dim bi3 As New BitmapImage
        bi3.BeginInit()
        bi3.UriSource = New Uri(NomImage, UriKind.RelativeOrAbsolute)
        bi3.EndInit()
        Return bi3
    End Function

    Private TempLockLink As Boolean
    Private Sub MenuContextuel_Opened(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        TempLockLink = True
    End Sub
    Private Sub MenuContextuel_Closed(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        TempLockLink = False
    End Sub

    '***********************************************************************************************
    '------------------------------MENU CONTEXTUEL ENTETE LISTE-------------------------------------
    '***********************************************************************************************
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
    Private Sub ReponseMenuHeader(ByVal Sender As Object, ByVal e As RoutedEventArgs)
        For Each i As GridViewColumn In CType(XMLBinding.View, GridView).Columns
            If CType(i.Header, GridViewColumnHeader).Content.ToString = CType(Sender, MenuItem).Header Then
                If CType(Sender, MenuItem).IsChecked Then i.Width = 0 Else i.Width = Double.NaN
                Exit For
            End If
        Next
    End Sub

    '**************************************************************************************************************
    '**************************************GESTION DU CLAVIER******************************************************
    '**************************************************************************************************************
    Private Sub Me_PreviewKeyDown(ByVal sender As Object, ByVal e As System.Windows.Input.KeyEventArgs) Handles Me.PreviewKeyDown
        Select Case e.Key
            Case Key.Delete
                If TypeOf (e.OriginalSource) Is ListViewItem Then
                    DeleteIdCollection()
                    e.Handled = True
                End If
            Case Key.A To Key.Z
                If TypeOf (e.OriginalSource) Is ListViewItem Or TypeOf (e.OriginalSource) Is ListView Then
                    If ColonneTriEnCours Is Nothing Then Exit Select
                    Dim NodeRecherche As String = ""
                    Select Case ColonneTriEnCours.Content.ToString
                        Case "Artiste"
                            NodeRecherche = "Artiste"
                        Case "Titre"
                            NodeRecherche = "Titre"
                        Case "Label"
                            NodeRecherche = "Label"
                    End Select
                    If NodeRecherche <> "" Then
                        If IconeDeTriEnCours.Direction = ListSortDirection.Ascending Then
                            Dim Debut As Integer
                            If XMLBinding.SelectedItem IsNot Nothing Then
                                If Asc(Left(XMLBinding.SelectedItem.SelectSingleNode(NodeRecherche).InnerText, 1)) <= Asc(Left(e.Key.ToString, 1)) Then
                                    Debut = XMLBinding.Items.IndexOf(XMLBinding.SelectedItem) + 1
                                Else
                                    Debut = 0
                                End If
                            Else
                                Debut = 0
                            End If
                            For i = Debut To XMLBinding.Items.Count - 1
                                If Left(XMLBinding.Items(i).SelectSingleNode(NodeRecherche).InnerText, 1) = Left(e.Key.ToString, 1) Then
                                    XMLBinding.SelectedItem = XMLBinding.Items(i)
                                    XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
                                    Exit For
                                End If
                            Next
                        Else
                            Dim Fin As Integer
                            If XMLBinding.SelectedItem IsNot Nothing Then
                                If Asc(Left(XMLBinding.SelectedItem.SelectSingleNode(NodeRecherche).InnerText, 1)) <= Asc(Left(e.Key.ToString, 1)) Then
                                    Fin = XMLBinding.Items.IndexOf(XMLBinding.SelectedItem) - 1
                                Else
                                    Fin = XMLBinding.Items.Count - 1
                                End If
                            Else
                                Fin = XMLBinding.Items.Count - 1
                            End If
                            For i = Fin To 0 Step -1
                                If Left(XMLBinding.Items(i).SelectSingleNode(NodeRecherche).InnerText, 1) = Left(e.Key.ToString, 1) Then
                                    XMLBinding.SelectedItem = XMLBinding.Items(i)
                                    XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
                                    Exit For
                                End If
                            Next

                        End If
                        e.Handled = True
                    End If
                End If
            Case Key.Up
                If XMLBinding.Items.IndexOf(XMLBinding.SelectedItem) > 0 Then
                    XMLBinding.SelectedItem = XMLBinding.Items(XMLBinding.Items.IndexOf(XMLBinding.SelectedItem) - 1)
                    XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
                End If
                e.Handled = True
            Case Key.Down
                If XMLBinding.Items.IndexOf(XMLBinding.SelectedItem) < XMLBinding.Items.Count - 1 Then
                    XMLBinding.SelectedItem = XMLBinding.Items(XMLBinding.Items.IndexOf(XMLBinding.SelectedItem) + 1)
                    XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
                End If
                e.Handled = True
        End Select
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
        BoutonsAjouterSupprimerFolders_Click(sender, e)
        StartPoint = e.GetPosition(XMLBinding)
        TypeObjetClic = e.OriginalSource
        If (TypeOf (e.OriginalSource) Is ScrollViewer) Then XMLBinding.SelectedItems.Clear()
        UpdateInfosIcons(e.OriginalSource)
        If TypeOf (e.OriginalSource) Is Image Then
            If TempLockLink Then
                e.Handled = True
                Exit Sub
            End If
            If CType(e.OriginalSource, Image).Name Like "tagLink*" Then
                If (Keyboard.GetKeyStates(Key.LeftCtrl) And KeyStates.Down) = 0 Then
                    UpdateRecherche(sender, e)
                    e.Handled = True
                Else
                    Dim Newurl As String = ""
                    Newurl = "http://www.discogs.com/release/" & CType(CType(e.OriginalSource, Image).Tag, XmlElement).InnerText
                    Dim NewURI As Uri = New Uri(Newurl)
                    RaiseEvent RequeteWebBrowser(NewURI)
                    e.Handled = True
                End If
            ElseIf CType(e.OriginalSource, Image).Name Like "Drapeau" Then
                If (Keyboard.GetKeyStates(Key.LeftCtrl) And KeyStates.Down) = 0 Then
                    UpdateRecherche(sender, e)
                    e.Handled = True
                End If
            End If
        End If
        If (TypeOf (e.OriginalSource) Is TextBlock) Then
            If TempLockLink Then
                e.Handled = True
                Exit Sub
            End If
            If CType(e.OriginalSource, TextBlock).Name Like "tagLink*" Then
                If (Keyboard.GetKeyStates(Key.LeftCtrl) And KeyStates.Down) = 0 Then
                    TexteLink = CType(e.OriginalSource, TextBlock)
                    UpdateRecherche(sender, e)
                    e.Handled = True
                Else
                    Dim Newurl As String = ""
                    Try
                        Select Case CType(e.OriginalSource, TextBlock).Name
                            Case "tagLinkArtiste"
                                Newurl = "http://www.discogs.com/search?q=" & CType(e.OriginalSource, TextBlock).Text & "&type=artists"
                            Case "tagLinkTitre"
                                Dim Chaine As String = Trim(ExtraitChaine(CType(e.OriginalSource, TextBlock).Text, "", "(", False))
                                If Chaine = "" Then Chaine = CType(e.OriginalSource, TextBlock).Text
                                Newurl = "http://www.discogs.com/search?q=" & Chaine & "&type=releases"
                            Case "tagLinkLabel"
                                Newurl = "http://www.discogs.com/search?q=" & CType(e.OriginalSource, TextBlock).Text & "&type=labels"
                            Case "tagLinkCatalogue"
                                Newurl = "http://www.discogs.com/search?q=" & CType(e.OriginalSource, TextBlock).Text & "&type=catalog#"
                            Case Else
                                Debug.Print(CType(e.OriginalSource, TextBlock).Name)
                                Newurl = "http://www.discogs.com/search?q=" & CType(e.OriginalSource, TextBlock).Text & "&type=all"
                        End Select
                        Dim NewURI As Uri = New Uri(Newurl)
                        RaiseEvent RequeteWebBrowser(NewURI)
                        e.Handled = False
                    Catch ex As Exception
                        wpfMsgBox.MsgBoxInfo("Lien URL non valide", "Le lien """ & Newurl & """ est non valide", Nothing)
                    End Try
                End If
            End If
        End If
        '  wpfMsgBox.MsgBoxInfo("INFO", e.OriginalSource.ToString, Nothing)
    End Sub
    Private Sub XMLBinding_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles XMLBinding.MouseLeave
        StartPoint = New Point
    End Sub
    Dim TexteLink As TextBlock = Nothing
    Private Sub XMLBinding_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles XMLBinding.MouseMove
        If TypeOf (e.OriginalSource) Is Image Then
            CType(e.OriginalSource, Image).Cursor = Cursors.Hand
            Exit Sub
        End If
        If TexteLink IsNot Nothing Then
            TexteLink.TextDecorations = Nothing
            TexteLink.Cursor = Cursors.Arrow
            TexteLink.Foreground = CType(TexteLink.Tag, Brush) ' Brushes.Black
            TexteLink = Nothing
        End If
        If TypeOf (e.OriginalSource) Is Border And TypeOf (TypeObjetClic) Is Border Then
            If CType(e.OriginalSource, Border).Name = "DragDropBorder" Then
                Dim MousePos As Point = e.GetPosition(XMLBinding)
                Dim Diff As Vector = StartPoint - MousePos
                If StartPoint.X <> 0 And StartPoint.Y <> 0 And (e.LeftButton = MouseButtonState.Pressed And Math.Abs(Diff.X) > 2 Or
                                                                Math.Abs(Diff.Y) > 2) Then
                    Dim FilesDataObject(XMLBinding.SelectedItems.Count - 1) As String
                End If
            End If
        Else
            If TypeOf (e.OriginalSource) Is TextBlock Then
                If (CType(e.OriginalSource, TextBlock).Name Like "tagLink*") Then
                    TexteLink = CType(e.OriginalSource, TextBlock)
                    TexteLink.Cursor = Cursors.Hand
                    Dim myCollection As New TextDecorationCollection()
                    myCollection.Add(New TextDecoration(TextDecorationLocation.Underline,
                                                                      Nothing, 0, TextDecorationUnit.Pixel,
                                                                      TextDecorationUnit.Pixel))
                    TexteLink.TextDecorations = myCollection
                    TexteLink.Tag = TexteLink.Foreground
                    If (Keyboard.GetKeyStates(Key.LeftShift) And KeyStates.Down) > 0 Then
                        TexteLink.Foreground = New SolidColorBrush(Color.FromRgb(&HFF, &HBF, &H9F))
                    ElseIf (Keyboard.GetKeyStates(Key.LeftCtrl) And KeyStates.Down) > 0 Then
                        TexteLink.Foreground = New SolidColorBrush(Color.FromRgb(&H9F, &HF8, &H34))
                    Else
                        TexteLink.Foreground = New SolidColorBrush(Color.FromRgb(&H9F, &HBF, &HFF))
                    End If
                End If
            End If
        End If
    End Sub

    Dim ActionCopierEnCours As Boolean
    Dim ActionCopierTagEnCours As Boolean
    Dim ExPosY As Double
    Private Sub XMLBinding_Drop(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles XMLBinding.Drop
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As XmlElement = Nothing
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            DonneeSurvolee = CType(ItemSurvole.Content, XmlElement)
            CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).Opacity = 0
        End If
        If (e.Data.GetDataPresent(DataFormats.Dib)) And (DonneeSurvolee IsNot Nothing) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            FileDrop(e, ItemSurvole)
        ElseIf (e.Data.GetDataPresent("tagID3FilesInfosDO")) Then
            FileDrop(e, )
        Else
            e.Effects = DragDropEffects.None
        End If
        If PlateformVista Then DropTargetHelper.Drop(e.Data, e.GetPosition(e.OriginalSource), e.Effects)
        e.Handled = True
    End Sub
    Private Sub XMLBinding_DragEnter(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles XMLBinding.DragEnter
        Dim FlagPlateformVista As Boolean
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As XmlElement = Nothing
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            DonneeSurvolee = CType(ItemSurvole.Content, XmlElement)
            CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).Opacity = 0.7
        ElseIf wpfApplication.FindAncestor(e.OriginalSource, "ListView") IsNot Nothing Then
            ElementSurvolePrecedent = Nothing
            TempsDeDebutSurvole = Now.Ticks
            ExPosY = 0
        End If
        If (e.Data.GetDataPresent(DataFormats.Dib)) And (DonneeSurvolee IsNot Nothing) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            Dim NomFichier As String = DonneeSurvolee.SelectSingleNode("Artiste").InnerText & " - " & _
                DonneeSurvolee.SelectSingleNode("Titre").InnerText
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                            e.Data, e.GetPosition(e.OriginalSource),
                                                            e.Effects, "Copier l'image vers %1", NomFichier)
        ElseIf (e.Data.GetDataPresent("tagID3FilesInfosDO")) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                            e.Data, e.GetPosition(e.OriginalSource),
                                                            e.Effects, "Ajouter un nouveau vinyl à la collection", "")
        Else
            e.Effects = DragDropEffects.None
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                            e.Data, e.GetPosition(e.OriginalSource),
                                                            e.Effects, "", "")
            e.Handled = True
        End If
    End Sub
    Private Sub XMLBinding_DragLeave(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles XMLBinding.DragLeave
        Dim ItemSurvole As ListViewItem
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).Opacity = 0
            'CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).BorderBrush = Brushes.Transparent
        ElseIf TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListView Then
            ElementSurvolePrecedent = Nothing
            ExPosY = 0
        End If
        If PlateformVista Then
            DropTargetHelper.DragLeave(e.Data)
        End If
        e.Handled = True
    End Sub
    Dim ElementSurvolePrecedent As XmlElement
    Dim TempsDeDebutSurvole As Long
    Private Sub XMLBinding_DragOver(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles XMLBinding.DragOver
        'Static compteur As Integer
        Dim FlagPlateformVista As Boolean
        Dim Tempo As TimeSpan = New TimeSpan(Now.Ticks - TempsDeDebutSurvole)
        If Tempo.TotalSeconds > 0.5 Then
            If (e.GetPosition(XMLBinding).Y <= ExPosY) And (e.GetPosition(XMLBinding).Y < 40) Then
                CType(XMLBinding.Template.FindName("GBScrollViewer", XMLBinding), ScrollViewer).LineUp()
            End If
            If (e.GetPosition(XMLBinding).Y >= ExPosY) And (e.GetPosition(XMLBinding).Y > (XMLBinding.ActualHeight - 40)) Then
                CType(XMLBinding.Template.FindName("GBScrollViewer", XMLBinding), ScrollViewer).LineDown()
            End If
        End If
        ExPosY = e.GetPosition(XMLBinding).Y
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As XmlElement = Nothing
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            DonneeSurvolee = CType(ItemSurvole.Content, XmlElement)
            'GESTION DU TEMPS DE SURVOLE DE L'ELEMENT POUR CHANGEMENT SELECTION
            If DonneeSurvolee.Equals(ElementSurvolePrecedent) Then
                Dim Intervale As TimeSpan = New TimeSpan(Now.Ticks - TempsDeDebutSurvole)
                If Intervale.TotalSeconds > 2 Then
                    XMLBinding.SelectedItem = DonneeSurvolee
                End If
            Else
                ElementSurvolePrecedent = DonneeSurvolee
                TempsDeDebutSurvole = Now.Ticks
            End If
        End If
        If (e.Data.GetDataPresent(DataFormats.Dib)) And (DonneeSurvolee IsNot Nothing) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragOver(e.GetPosition(e.OriginalSource), e.Effects)
        ElseIf (e.Data.GetDataPresent("tagID3FilesInfosDO")) Then
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragOver(e.GetPosition(e.OriginalSource), e.Effects)
        Else
            e.Effects = DragDropEffects.None
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                e.Data, e.GetPosition(e.OriginalSource),
                                                                e.Effects, "", "")
        End If
        e.Handled = True
    End Sub

    'FONCTION DE TRAITEMENT DU DROP SUR LA LISTE
    Private Sub FileDrop(ByVal e As System.Windows.DragEventArgs, Optional ByVal Item As ListViewItem = Nothing)
        Dim Formats As String() = e.Data.GetFormats()
        Dim Chemin As String = ""
        Dim NomDirectory As String = ""
        Dim NomFichier As String = ""
        If e.Data.GetDataPresent(DataFormats.Dib) Then
            If Item IsNot Nothing Then
                Dim DonneeSurvolee As XmlElement = CType(Item.Content, XmlElement)
                Dim Data As Byte() = TagID3.tagID3Object.FonctionUtilite.ConvertDibstreamToJpegdata(
                                            CType(e.Data.GetData(DataFormats.Dib), System.IO.MemoryStream))
                If Data IsNot Nothing Then
                    If DonneeSurvolee.SelectSingleNode("id").InnerText <> "" Then
                        Dim NomImageExistante As String = DonneeSurvolee.SelectSingleNode("Image").InnerText
                        Dim Index = ExtraitChaine(NomImageExistante, "-", "", , False)
                        If Index = "" Then Index = "1" Else Index = CStr(CInt(Index) + 1)
                        Dim MemId = DonneeSurvolee.SelectSingleNode("id").InnerText()
                        EnregistrerImage(MemId & "-" & Index, Data)
                        DonneeSurvolee.SelectSingleNode("Image").InnerText = MemId & "-" & Index
                        Dim ImageUpdate As Controls.Image = CType(wpfApplication.FindChild(Item, "TagLinkImagePochette"), Controls.Image)
                        ImageUpdate.GetBindingExpression(Controls.Image.SourceProperty).UpdateTarget()

                    End If
                End If
            End If
        End If
        If e.Data.GetDataPresent("tagID3FilesInfosDO2") Then
            Dim Info As List(Of tagID3FilesInfosDO) = CType(e.Data.GetData("tagID3FilesInfosDO2"), List(Of tagID3FilesInfosDO))
            For Each i In Info
                CreateIdCollection(i)
            Next
        ElseIf e.Data.GetDataPresent("tagID3FilesInfosDO") Then
            Dim Info As tagID3FilesInfosDO = CType(e.Data.GetData("tagID3FilesInfosDO"), tagID3FilesInfosDO)
            CreateIdCollection(Info)
        End If
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DES MISES A JOUR DE LA LISTE DES VINYLS---------------
    '***********************************************************************************************
    Private Sub Filtres_MouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) _
                            Handles FiltreVinylAPayer.MouseLeftButtonDown,
                                    FiltreVinylForSale.MouseLeftButtonDown,
                                    FiltreVinylCollection.MouseLeftButtonDown,
                                    FiltreVinylWanted.MouseLeftButtonDown,
                                    FiltreVinylDiscogs.MouseLeftButtonDown,
                                    FiltreVinylARecevoir.MouseLeftButtonDown
        If TempLockLink Then Exit Sub
        Dim NodeSelectionne As XmlElement = CType(XMLBinding.SelectedItem, XmlElement)
        Dim Selection As String = ""
        If NodeSelectionne IsNot Nothing Then
            If NodeSelectionne.SelectSingleNode("id").InnerText <> "" Then
                Selection = "id:" & NodeSelectionne.SelectSingleNode("id").InnerText
            Else
                Selection = "artiste:" & NodeSelectionne.SelectSingleNode("Artiste").InnerText
            End If
        End If
        If CType(sender, Image).Opacity < 1 Then
            CType(sender, Image).Opacity = 1
            ListeDesRecherches.Add("IconOn:" & CType(sender, Image).Name & "\\{" & Selection)
            RecherchePrecedente.IsEnabled = True
        Else
            CType(sender, Image).Opacity = 0.3
            ListeDesRecherches.Add("IconOff:" & CType(sender, Image).Name & "\\{" & Selection)
            RecherchePrecedente.IsEnabled = True
        End If
        UpdateFiltre()
    End Sub

    Private Sub RecherchePrecedente_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles RecherchePrecedente.Click
        If TempLockLink Then Exit Sub
        If ListeDesRecherches.Count > 0 Then
            Dim Tab() As String = Split(ListeDesRecherches.Last, ":")
            Select Case Tab(0)
                Case "Recherche"
                    If ListeDesRecherches.Count > 1 Then
                        Dim TextePrecedent As String = ""
                        For h = ListeDesRecherches.Count - 2 To 0 Step -1
                            If InStr(ListeDesRecherches(h), "Recherche:") Then
                                TextePrecedent = ExtraitChaine(ListeDesRecherches(h), "Recherche:", "\\{", 10)
                                Exit For
                            End If
                        Next
                        RechercheArtiste.Text = TextePrecedent
                    Else
                        RechercheArtiste.Text = ""
                    End If
                Case "FolderPre"
                    LockUpdateFolderSearch = True
                    CollectionFolderList.Text = ExtraitChaine(Tab(1), "", "\\{")
                    LockUpdateFolderSearch = False
                Case "IconOn"
                    Select Case ExtraitChaine(Tab(1), "", "\\{")
                        Case "FiltreVinylAPayer"
                            FiltreVinylAPayer.Opacity = 0.3
                        Case "FiltreVinylForSale"
                            FiltreVinylForSale.Opacity = 0.3
                        Case "FiltreVinylCollection"
                            FiltreVinylCollection.Opacity = 0.3
                        Case "FiltreVinylWanted"
                            FiltreVinylWanted.Opacity = 0.3
                        Case "FiltreVinylDiscogs"
                            FiltreVinylDiscogs.Opacity = 0.3
                        Case "FiltreVinylARecevoir"
                            FiltreVinylARecevoir.Opacity = 0.3
                    End Select
                Case "IconOff"
                    Select Case ExtraitChaine(Tab(1), "", "\\{")
                        Case "FiltreVinylAPayer"
                            FiltreVinylAPayer.Opacity = 1
                        Case "FiltreVinylForSale"
                            FiltreVinylForSale.Opacity = 1
                        Case "FiltreVinylCollection"
                            FiltreVinylCollection.Opacity = 1
                        Case "FiltreVinylWanted"
                            FiltreVinylWanted.Opacity = 1
                        Case "FiltreVinylDiscogs"
                            FiltreVinylDiscogs.Opacity = 1
                        Case "FiltreVinylARecevoir"
                            FiltreVinylARecevoir.Opacity = 1
                    End Select
            End Select
            UpdateFiltre()
            Dim Selection As String = ExtraitChaine(ListeDesRecherches.Last, "\\{", "", 3, True)
            If Selection <> "" Then
                If Left(Selection, 2) = "id" Then
                    Dim RepereSelection As String = ExtraitChaine(Selection, "id:", "", 3, True)
                    For Each i As XmlElement In XMLBinding.Items
                        If i.SelectSingleNode("id").InnerText = RepereSelection Then XMLBinding.SelectedItem = i
                    Next
                Else
                    Dim ArtisteSelection As String = ExtraitChaine(Selection, "artiste:", "", 8, True)
                    For Each i As XmlElement In XMLBinding.Items
                        If i.SelectSingleNode("Artiste").InnerText = ArtisteSelection Then XMLBinding.SelectedItem = i
                    Next
                End If
                XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
            End If
            ListeDesRecherches.RemoveRange(ListeDesRecherches.Count - 1, 1)
            If ListeDesRecherches.Count = 0 Then
                RecherchePrecedente.IsEnabled = False
            End If
        End If
    End Sub
    Private Sub AnnuleRechercheEnCours_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles AnnuleRechercheEnCours.Click
        If TempLockLink Then Exit Sub
        'If Not AnnuleRechercheEnCours.IsChecked Then
        ListeDesRecherches.Clear()
        RechercheArtiste.Text = ""
        FiltreVinylAPayer.Opacity = 0.3
        FiltreVinylForSale.Opacity = 0.3
        FiltreVinylCollection.Opacity = 0.3
        FiltreVinylWanted.Opacity = 0.3
        FiltreVinylDiscogs.Opacity = 0.3
        FiltreVinylARecevoir.Opacity = 0.3
        LockUpdateFolderSearch = True
        CollectionFolderList.Text = "All"
        LockUpdateFolderSearch = False
        UpdateFiltre()
        RecherchePrecedente.IsEnabled = False
        XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
        '    RecherchePrecedente.Visibility = Visibility.Visible
        ' End If
    End Sub
    Private FiltreBloque As Boolean = False
    Public Function FiltreVinyls(ByVal ChaineRecherche As String, ByVal NewRequete As Boolean, Optional ByVal RequeteInterne As Boolean = False) As Integer
        If (Me.IsLoaded) And (Not FiltreBloque) And (IndicateurRechercheDupliquer.IsChecked Or RequeteInterne) Then
            If (NewRequete) Then ' Or (Microsoft.VisualBasic.Left(RechercheArtiste.Text, 1) <> "?") Then
                RechercheArtiste.Text = ChaineRecherche
            Else
                RechercheArtiste.Text = RechercheArtiste.Text & "," & ChaineRecherche
            End If
            If RechercheArtiste.Text <> "" Then
                Dim NodeSelectionne As XmlElement = CType(XMLBinding.SelectedItem, XmlElement)
                Dim Selection As String = ""
                If NodeSelectionne IsNot Nothing Then
                    If NodeSelectionne.SelectSingleNode("id").InnerText <> "" Then
                        Selection = "id:" & NodeSelectionne.SelectSingleNode("id").InnerText
                    Else
                        Selection = "artiste:" & NodeSelectionne.SelectSingleNode("Artiste").InnerText
                    End If
                End If
                ListeDesRecherches.Add("Recherche:" & RechercheArtiste.Text & "\\{" & Selection)
                RecherchePrecedente.IsEnabled = True
            End If
            UpdateFiltre()
            Return XMLBinding.Items.Count
        Else
            Return -1
        End If
    End Function
    Private Sub EnvoieRequeteRecherche(ByVal ChaineRecherche As String, ByVal NewRequete As Boolean)
        FiltreBloque = True
        RaiseEvent RequeteRecherche(ChaineRecherche, NewRequete)
        FiltreBloque = False
    End Sub
    Private Sub RechercheArtiste_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Input.KeyEventArgs) Handles RechercheArtiste.KeyDown
        Select Case e.Key
            Case Key.Enter
                If IndicateurRechercheDupliquer.IsChecked Then
                    EnvoieRequeteRecherche(RechercheArtiste.Text, True)
                End If
                RechercheLocale.IsChecked = True
                If RechercheArtiste.Text <> "" Then
                    Dim NodeSelectionne As XmlElement = CType(XMLBinding.SelectedItem, XmlElement)
                    Dim Selection As String = ""
                    If NodeSelectionne IsNot Nothing Then
                        If NodeSelectionne.SelectSingleNode("id").InnerText <> "" Then
                            Selection = "id:" & NodeSelectionne.SelectSingleNode("id").InnerText
                        Else
                            Selection = "artiste:" & NodeSelectionne.SelectSingleNode("Artiste").InnerText
                        End If
                    End If
                    ListeDesRecherches.Add("Recherche:" & RechercheArtiste.Text & "\\{" & Selection)
                    RecherchePrecedente.IsEnabled = True
                Else
                    ListeDesRecherches.Clear()
                    RecherchePrecedente.IsEnabled = False
                End If
                UpdateFiltre()
                '   Case Key.LeftCtrl
                '       IndicateurRechercheDupliquer.IsChecked = Not IndicateurRechercheDupliquer.IsChecked
        End Select
    End Sub
    'PROCEDURES D'APPEL RECHERCHE BIBLIOTHEQUE
    Private Sub UpdateRecherche(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        If TypeOf (e.OriginalSource) Is Image Then
            Dim Lien As Image = CType(e.OriginalSource, Image)
            Dim ShiftDown As Boolean = (Keyboard.GetKeyStates(Key.LeftShift) And KeyStates.Down) > 0
            Select Case Lien.Name
                Case "TagLinkImagePochette"
                    If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("id:" & CType(CType(e.OriginalSource, Image).Tag, XmlElement).InnerText, Not ShiftDown)
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("id:" & CType(CType(e.OriginalSource, Image).Tag, XmlElement).InnerText, Not ShiftDown, True)
                Case "Drapeau"
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("state:" & CType(CType(e.OriginalSource, Image).Tag, XmlElement).InnerText, Not ShiftDown, True)
            End Select
        End If
        If TypeOf (e.OriginalSource) Is TextBlock Then
            Dim Lien As TextBlock = CType(e.OriginalSource, TextBlock)
            If Lien.Text <> "" Then
                Dim ShiftDown As Boolean = (Keyboard.GetKeyStates(Key.LeftShift) And KeyStates.Down) > 0
                Select Case Lien.Name
                    Case "tagLinkArtiste"
                        If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("artiste:" & Lien.Text, Not ShiftDown)
                        If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("artiste:" & Lien.Text, Not ShiftDown, True)
                    Case "tagLinkTitre"
                        Dim Chaine As String = Trim(ExtraitChaine(Lien.Text, "", "(", False))
                        If Chaine = "" Then Chaine = Lien.Text
                        If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("titre:" & Chaine, Not ShiftDown)
                        If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("titre:" & Chaine, Not ShiftDown, True)
                    Case "tagLinkLabel"
                        If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("label:" & Lien.Text, Not ShiftDown)
                        If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("label:" & Lien.Text, Not ShiftDown, True)
                    Case "tagLinkCatalogue"
                        If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("catalogue:" & Lien.Text, Not ShiftDown)
                        If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("catalogue:" & Lien.Text, Not ShiftDown, True)
                    Case "tagLinkAnnee"
                        If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("annee:" & Lien.Text, Not ShiftDown)
                        If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("annee:" & Lien.Text, Not ShiftDown, True)
                    Case "tagLinkStyle"
                        If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("style:" & Lien.Text, Not ShiftDown)
                        If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("style:" & Lien.Text, Not ShiftDown, True)
                End Select
            End If
        End If
    End Sub

    Private Sub UpdateFiltre(Optional FiltreFolder As String = "")
        'PRINCIPE DE CODAGE DE LA REQUETE
        ' syntaxe de la requete 'Nom critere:Valeur cherchee,Autre critere: Valeur cherchee...'
        ' pour une recherche non stricte ajouter '*' en debut du nom du critère
        ' pour une recherche plus stricte ajouter '+' en debut du nom du critère
        ' liste des criteres : id - artiste - titre - label - catalogue - pays - annee - style
        Dim Chaine As String = RechercheArtiste.Text
        XMLBinding.Items.Filter = Function(ob As Object)
                                      Dim Vinyl As XmlElement = TryCast(ob, XmlElement)
                                      Dim TabBranche() As String = Split(Chaine, "/")
                                      Dim MemResultatOR As Boolean = False
                                      For Each Branche In TabBranche
                                          Dim Resultat As Boolean = False
                                          If (TabBranche.Count = 1) And (Branche = "") Then MemResultatOR = True
                                          Dim TabCriteres() As String = Split(Branche, ",")
                                          Dim MemResultatAnd As Boolean = True
                                          For Each j In TabCriteres
                                              Dim NomCritere As String = Trim(ExtraitChaine(j, "", ":"))
                                              Dim ChaineRecherche As String = Trim(ExtraitChaine(j, ":", "", , True))
                                              If j <> "" Then Resultat = True Else Resultat = False
                                              Select Case NomCritere
                                                  Case "id", "+id", "*id"
                                                      If Vinyl.SelectSingleNode("id").InnerText <> ChaineRecherche Then Resultat = False
                                                  Case "artiste", "a", "+artiste", "+a", "*artiste", "*a"
                                                      If Left(NomCritere, 1) = "+" Then
                                                          If Vinyl.SelectSingleNode("Artiste").InnerText <> ChaineRecherche Then Resultat = False
                                                      ElseIf Left(NomCritere, 1) = "*" Then
                                                          For Each i In Split(ChaineRecherche)
                                                              If InStr(Vinyl.SelectSingleNode("Artiste").InnerText, Trim(i), CompareMethod.Text) = 0 Then Resultat = False
                                                          Next
                                                      Else
                                                          If InStr(Vinyl.SelectSingleNode("Artiste").InnerText, ChaineRecherche, CompareMethod.Text) = 0 Then Resultat = False
                                                      End If
                                                  Case "titre", "t", "+titre", "+t", "*titre", "*t"
                                                      If Left(NomCritere, 1) = "+" Then
                                                          If Vinyl.SelectSingleNode("Titre").InnerText <> ChaineRecherche Then Resultat = False
                                                      ElseIf Left(NomCritere, 1) = "*" Then
                                                          For Each i In Split(ChaineRecherche)
                                                              If InStr(Vinyl.SelectSingleNode("Titre").InnerText, Trim(i), CompareMethod.Text) = 0 Then Resultat = False
                                                          Next
                                                      Else
                                                          If InStr(Vinyl.SelectSingleNode("Titre").InnerText, ChaineRecherche, CompareMethod.Text) = 0 Then Resultat = False
                                                      End If
                                                  Case "label", "l", "+label", "+l", "*label", "*l"
                                                      If Left(NomCritere, 1) = "+" Then
                                                          If Vinyl.SelectSingleNode("Label").InnerText <> ChaineRecherche Then Resultat = False
                                                      ElseIf Left(NomCritere, 1) = "*" Then
                                                          For Each i In Split(ChaineRecherche)
                                                              If InStr(Vinyl.SelectSingleNode("Label").InnerText, Trim(i), CompareMethod.Text) = 0 Then Resultat = False
                                                          Next
                                                      Else
                                                          If (ChaineRecherche = "") Then
                                                              If (Vinyl.SelectSingleNode("Label").InnerText <> "") Then Resultat = False
                                                          Else
                                                              If InStr(Vinyl.SelectSingleNode("Label").InnerText, ChaineRecherche, CompareMethod.Text) = 0 Then Resultat = False
                                                          End If
                                                      End If
                                                  Case "catalogue", "c", "+catalogue", "+c", "*catalogue", "*c"
                                                      For Each i In Split(ChaineRecherche)
                                                          If InStr(Vinyl.SelectSingleNode("Catalogue").InnerText, Trim(i), CompareMethod.Text) = 0 Then Resultat = False
                                                      Next
                                                  Case "annee", "y", "+annee", "+y", "*annee", "*y"
                                                      If Vinyl.SelectSingleNode("Annee").InnerText <> ChaineRecherche Then Resultat = False
                                                  Case "style", "s", "+style", "+s", "*style", "*s"
                                                      If Left(NomCritere, 1) = "+" Then
                                                          If Vinyl.SelectSingleNode("Style").InnerText <> ChaineRecherche Then Resultat = False
                                                      ElseIf Left(NomCritere, 1) = "*" Then
                                                          For Each i In Split(ChaineRecherche)
                                                              If InStr(Vinyl.SelectSingleNode("Style").InnerText, Trim(i), CompareMethod.Text) = 0 Then Resultat = False
                                                          Next
                                                      Else
                                                          If InStr(Vinyl.SelectSingleNode("Style").InnerText, ChaineRecherche, CompareMethod.Text) = 0 Then Resultat = False
                                                      End If
                                                  Case "state", "+state", "*state"
                                                      If Vinyl.SelectSingleNode("Pays").InnerText <> ChaineRecherche Then Resultat = False
                                                  Case Else
                                                      For Each i In Split(ChaineRecherche)
                                                          If InStr(Vinyl.SelectSingleNode("Artiste").InnerText, Trim(i), CompareMethod.Text) = 0 Then
                                                              Resultat = False
                                                          End If
                                                      Next
                                              End Select
                                              MemResultatAnd = MemResultatAnd And Resultat
                                          Next
                                          MemResultatOR = MemResultatAnd Or MemResultatOR
                                      Next
                                      Dim IconResultat As Boolean = True
                                      If (FiltreVinylCollection.Opacity = 1) And Vinyl.SelectSingleNode("VinylCollection").InnerText <> "True" Then IconResultat = False
                                      If (FiltreVinylDiscogs.Opacity = 1) And Vinyl.SelectSingleNode("VinylDiscogs").InnerText <> "True" Then IconResultat = False
                                      If (FiltreVinylWanted.Opacity = 1) And Vinyl.SelectSingleNode("VinylWanted").InnerText <> "True" Then IconResultat = False
                                      If (FiltreVinylForSale.Opacity = 1) And Vinyl.SelectSingleNode("VinylForSale").InnerText <> "True" Then IconResultat = False
                                      If (FiltreVinylARecevoir.Opacity = 1) And Vinyl.SelectSingleNode("VinylARecevoir").InnerText <> "True" Then IconResultat = False
                                      If (FiltreVinylAPayer.Opacity = 1) And Vinyl.SelectSingleNode("VinylAPayer").InnerText <> "True" Then IconResultat = False
                                      Dim TexteFolder As String
                                      If FiltreFolder <> "" Then TexteFolder = FiltreFolder Else TexteFolder = CollectionFolderList.Text
                                      If TexteFolder <> "All" Then
                                          If (TexteFolder <> Vinyl.SelectSingleNode("idFolder").InnerText) Then IconResultat = False
                                      End If
                                      Return (MemResultatOR And IconResultat)
                                  End Function
        NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
    End Sub

    'Dim TexteModifie As Boolean = False
    Private Sub TexteMisAJour(ByVal sender As System.Object, ByVal e As System.Windows.Controls.TextChangedEventArgs)
        '    TexteModifie = True
    End Sub

    Private Sub BPDiscogsInfos_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        If XMLBinding.SelectedItems.Count = 1 Then
            idRelease = CType(XMLBinding.SelectedItem, XmlElement).SelectSingleNode("id").InnerText
            RaiseEvent RequeteInfosDiscogs(idRelease, NavigationDiscogsType.release)
        End If
    End Sub
    Dim idRelease As String
    Private Sub XMLBinding_SelectionChanged(ByVal sender As Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles XMLBinding.SelectionChanged
        TimerDiscogs.Stop()
        If XMLBinding.SelectedItems.Count = 1 Then
            TimerDiscogs.Interval = 300
            TimerDiscogs.Start()
            idRelease = CType(XMLBinding.SelectedItem, XmlElement).SelectSingleNode("id").InnerText
        End If
    End Sub
    Delegate Sub TraitementTimer()
    Private Sub TimerDiscogs_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles TimerDiscogs.Elapsed
        TimerDiscogs.Stop()
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New TraitementTimer(Sub()
                                                             RaiseEvent RequeteInfosDiscogs(idRelease, NavigationDiscogsType.updaterelease)
                                                         End Sub))
    End Sub

    '***********************************************************************************************
    '---------------------------------MISE A JOUR DES ICONES DANS LISTE-----------------------
    '***********************************************************************************************
    Private Sub UpdateInfosIcons(ByVal OriginalSource As Object)
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As XmlElement = Nothing
        Try
            If TypeOf (OriginalSource) Is Image Then
                If TypeOf (XMLBinding.ContainerFromElement(OriginalSource)) Is ListViewItem Then
                    ItemSurvole = CType(XMLBinding.ContainerFromElement(OriginalSource), ListViewItem)
                    DonneeSurvolee = CType(ItemSurvole.Content, XmlElement)
                    Dim NomDonnee As String = ExtraitChaine(CType(OriginalSource, Image).Name, "Tag", "", 3)
                    Dim Valeur As String = DonneeSurvolee.SelectSingleNode(NomDonnee).InnerText
                    If Valeur = "" Or (Valeur = "False") Then
                        If NomDonnee = "VinylCollection" Then Exit Sub ' RecuperationInfos(DonneeSurvolee)
                        If NomDonnee = "VinylDiscogs" Then
                            DiscogsServer.RequestAdd_CollectionId(Application.Config.user_name, DonneeSurvolee.SelectSingleNode("id").InnerText,
                                                                New DelegateRequestResult(AddressOf DiscogsServerAddIdResultNotify))
                        End If
                        DonneeSurvolee.SelectSingleNode(NomDonnee).InnerText = "True"
                    ElseIf Valeur = "True" Then
                        If NomDonnee = "VinylCollection" Then Exit Sub
                        ' If DonneeSurvolee.SelectSingleNode("Label").InnerText <> "" Then
                        ' If Not wpfMsgBox.MsgBoxQuestion("MISE A JOUR DES INFORMATIONS", "Voulez-vous mettre à jour les informations du vinyl sélectionné?", ItemSurvole, _
                        '                                 IIf(DonneeSurvolee.SelectSingleNode("id").InnerText <> "", "Mise a jour suivant id '" & DonneeSurvolee.SelectSingleNode("id").InnerText & "'", _
                        '                                        "A partir du titre sélectionné dans la liste des fichiers")) Then Exit Sub
                        ' RecuperationInfos(DonneeSurvolee)
                        ' Exit Sub
                        'End If
                        'End If
                        If NomDonnee = "VinylDiscogs" Then
                            DiscogsServer.RequestDelete_CollectionId(Application.Config.user_name, DonneeSurvolee.SelectSingleNode("id").InnerText,
                                                                New DelegateRequestResult(AddressOf DiscogsServerDeleteIdResultNotify))
                        End If
                        DonneeSurvolee.SelectSingleNode(NomDonnee).InnerText = "False"
                    End If
                End If
            End If
        Catch ex As Exception
        End Try
    End Sub
    Private Sub RecuperationInfos(ByVal DonneeSurvolee As XmlElement)
        If DonneeSurvolee IsNot Nothing Then
            If DonneeSurvolee.SelectSingleNode("id").InnerText() = "" Then
                Dim FichierALier As String = ""
                RaiseEvent UpdateLink(FichierALier)
                If FichierALier <> "" Then
                    Dim InfoFichiers As New tagID3FilesInfos(FichierALier)
                    DonneeSurvolee.SelectSingleNode("id").InnerText() = InfoFichiers.idRelease
                    DonneeSurvolee.SelectSingleNode("Catalogue").InnerText() = InfoFichiers.Catalogue
                    DonneeSurvolee.SelectSingleNode("Label").InnerText() = InfoFichiers.Label
                    DonneeSurvolee.SelectSingleNode("Artiste").InnerText() = InfoFichiers.Artiste
                    DonneeSurvolee.SelectSingleNode("Titre").InnerText() = Trim(ExtraitChaine(InfoFichiers.Titre, "", "[", , True))
                    DonneeSurvolee.SelectSingleNode("Annee").InnerText() = InfoFichiers.Annee
                    DonneeSurvolee.SelectSingleNode("Style").InnerText() = InfoFichiers.Style
                End If
            Else
                Dim RequeteDiscogs As Discogs = New Discogs(DonneeSurvolee.SelectSingleNode("id").InnerText())
                Dim CreationRelease As Boolean
                If DonneeSurvolee.SelectSingleNode("Titre").InnerText() = "" Then CreationRelease = True
                If RequeteDiscogs.release.artiste.nom <> "" Then
                    Dim MemId = DonneeSurvolee.SelectSingleNode("id").InnerText()
                    'DonneeSurvolee.SelectSingleNode("id").InnerText() = ""
                    DonneeSurvolee.SelectSingleNode("Catalogue").InnerText() = RequeteDiscogs.release.label.catalogue
                    DonneeSurvolee.SelectSingleNode("Label").InnerText() = Trim(ExtraitChaine(RequeteDiscogs.release.label.nom, "", "(", , True))
                    DonneeSurvolee.SelectSingleNode("Artiste").InnerText() = Trim(ExtraitChaine(RequeteDiscogs.release.artiste.nom, "", "(", , True))
                    DonneeSurvolee.SelectSingleNode("Titre").InnerText() = RequeteDiscogs.release.titre
                    DonneeSurvolee.SelectSingleNode("Pays").InnerText() = RequeteDiscogs.release.pays
                    DonneeSurvolee.SelectSingleNode("Annee").InnerText() = Trim(ExtraitChaine(RequeteDiscogs.release.annee, "", "-", , True))
                    DonneeSurvolee.SelectSingleNode("Style").InnerText() = RequeteDiscogs.release.style
                    DonneeSurvolee.SelectSingleNode("Format").InnerText() = RequeteDiscogs.release.format
                    ' If RequeteDiscogs.release.images.Count > 0 Then
                    ' RecupereImage(MemId, RequeteDiscogs.release.images(0))
                    'End If
                    DonneeSurvolee.SelectSingleNode("Image").InnerText() = MemId
                    If XMLBinding.SelectedIndex >= 0 And Not CreationRelease Then
                        Dim Element = XMLBinding.ItemContainerGenerator.ContainerFromIndex(XMLBinding.SelectedIndex)
                        If Element IsNot Nothing Then
                            Dim ImageUpdate As Controls.Image = CType(wpfApplication.FindChild(Element, "TagLinkImagePochette"), Controls.Image)
                            ImageUpdate.GetBindingExpression(Controls.Image.SourceProperty).UpdateTarget()
                        End If
                    End If
                    If DonneeSurvolee.SelectSingleNode("Pistes") IsNot Nothing Then DonneeSurvolee.SelectSingleNode("Pistes").RemoveAll()
                    For Each i In RequeteDiscogs.release.pistes
                        Dim NewNode As XmlElement = DonneeSurvolee.OwnerDocument.CreateElement("Piste")
                        NewNode.InnerText = i.nomPisteConcat
                        DonneeSurvolee.SelectSingleNode("Pistes").AppendChild(NewNode)
                    Next

            End If
                End If
        End If
    End Sub

    Private Sub RecupereImage(ByVal id As String, ByVal lien As DiscogsImage)
        Dim PathFichier As String
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_IMAGESVINYLS
        If Not Directory.Exists(RepDest) Then
            Directory.CreateDirectory(RepDest)
        End If
        PathFichier = RepDest & "\" & id & ".jpg"
        Dim WebClient As New System.Net.WebClient
        WebClient.Headers.Add("user-agent", "GBPlayer3")
        Try
            WebClient.DownloadFile(lien.urlImage, PathFichier)
        Catch ex As Exception
        End Try
        'End If
    End Sub
    Private Function EnregistrerImage(ByVal id As String, ByVal Data As Byte()) As String
        Dim PathFichier As String
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_IMAGESVINYLS
        If Not Directory.Exists(RepDest) Then
            Directory.CreateDirectory(RepDest)
        End If
        PathFichier = RepDest & "\" & id & ".jpg"
        Try
            TagID3.tagID3Object.FonctionUtilite.SaveImage(Data, PathFichier)
        Catch ex As Exception
        End Try
    End Function
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
        XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
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
            XMLBinding.Items.SortDescriptions.Add(New SortDescription("Titre", ListSortDirection.Ascending))
        End If
    End Sub

End Class
