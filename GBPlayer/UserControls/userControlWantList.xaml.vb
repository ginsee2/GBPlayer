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

Public Class userControlWantList
    Implements iNotifyWantListUpdate


    Public Event RequeteRecherche(ByVal ChaineRecherche As String, ByVal NewRequete As Boolean)
    Public Event RequeteWebBrowser(ByVal url As Uri)
    Public Event IdWantlistAdded(ByVal id As String, ByVal Transmitter As iNotifyWantListUpdate) Implements iNotifyWantListUpdate.IdWantlistAdded
    Public Event IdWantlistRemoved(ByVal id As String, ByVal Transmitter As iNotifyWantListUpdate) Implements iNotifyWantListUpdate.IdWantlistRemoved
    Public Event IdWantlistUpdated(ByVal Document As XmlDocument, ByVal Transmitter As iNotifyWantListUpdate) Implements iNotifyWantListUpdate.IdWantlistUpdated

    Private ListeDesRecherches As List(Of String) = New List(Of String)
    Public Property DisplayValidation As Boolean
    Public ReadOnly Property GetXmlDocument As XmlDocument
        Get
            Return DataProvider.Document
        End Get
    End Property

    Dim MenuContextuel As New ContextMenu
    Private PathFichierWantList As String
    Private Const GBAU_NOMDOSSIER_WANTLIST = "GBDev\GBPlayer\Data\Discogs"
    Private Const GBAU_NOMFICHIER_WANTLIST = "DiscogsWantList"
    Private Delegate Sub NoArgDelegate()

    '***********************************************************************************************
    '---------------------------------INITIALISATION DE LA CLASSE-----------------------------------
    '***********************************************************************************************
    Public Sub New()
        ' Cet appel est requis par le concepteur.
        InitializeComponent()
        LinkFileWantList()
    End Sub
    Private Sub LinkFileWantList()
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_WANTLIST
        If Directory.Exists(RepDest) Then
            PathFichierWantList = String.Format("{0}\{1}_{2}.xml", {RepDest, GBAU_NOMFICHIER_WANTLIST, Application.Config.user_name})
        End If
        If PathFichierWantList IsNot Nothing Then DataProvider.Source = New Uri(PathFichierWantList)
    End Sub
    Private Sub gbListeCollection_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Loaded
        If Not DisplayValidation Then Exit Sub
        If DataProvider.Document Is Nothing Then LinkFileWantList()
        Dim SauvegardeWantList As New Dictionary(Of String, GridViewColumn)
        For Each i As GridViewColumn In CType(XMLBinding.View, GridView).Columns
            SauvegardeWantList.Add(CType(i.Header, GridViewColumnHeader).Content.ToString, i)
        Next
        CType(XMLBinding.View, GridView).Columns.Clear()
        Application.Config.wantList_columns.ForEach(Sub(c As ConfigApp.ColumnDescription)
                                                        Try
                                                            Dim NomColonne As String = c.Name
                                                            Dim Dimension As Double = 0
                                                            Try
                                                                Dimension = c.Size
                                                            Catch ex As Exception
                                                                Dimension = Double.NaN
                                                            End Try
                                                            Dim Colonne As GridViewColumn = SauvegardeWantList.Item(NomColonne)
                                                            SauvegardeWantList.Remove(NomColonne)
                                                            Colonne.Width = Dimension
                                                            CType(XMLBinding.View, GridView).Columns.Add(Colonne)
                                                        Catch ex As Exception
                                                        End Try
                                                    End Sub)
        If SauvegardeWantList.Count > 0 Then
            For Each i In SauvegardeWantList
                CType(XMLBinding.View, GridView).Columns.Add(i.Value)
            Next
        End If
        ActiveTri(Application.Config.wantList_sortColumn.Name, Application.Config.wantList_sortColumn.SortDirection)
        If System.Environment.OSVersion.Platform = PlatformID.Win32NT Then
            If System.Environment.OSVersion.Version.Major > 5 Then PlateformVista = True
        End If
        NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
    End Sub
    Private Sub gbListeCollection_Unloaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Unloaded
        If Not DisplayValidation Then Exit Sub
        SaveConfiguration()
    End Sub
    Public Sub SaveConfiguration()
        If Not DisplayValidation Then Exit Sub
        ConfigApp.UpdateListeColonnes(Application.Config.wantList_columns, CType(XMLBinding.View, GridView).Columns)
        If ColonneTriEnCours IsNot Nothing Then
            Application.Config.wantList_sortColumn = New ConfigApp.DescriptionTri(ColonneTriEnCours.Tag, IconeDeTriEnCours.Direction)
        End If
        Dim source As String = DataProvider.Source.LocalPath
        If DataProvider.Document IsNot Nothing Then
            'DataProvider.Document.Save(source)
        End If
    End Sub

    '***********************************************************************************************
    '---------------------------------DESTRUCTION DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Protected Overrides Sub Finalize()
        If DataProvider.Document IsNot Nothing Then
            Dim source As String = DataProvider.Source.LocalPath
            DataProvider.Document.Save(source)
        End If
        MyBase.Finalize()
    End Sub

    '**************************************************************************************************************
    '***********************************************GESTION UPDATE WANTLIST****************************************
    '**************************************************************************************************************
    Public Sub UpdateIdWantList(ByVal idRelease As String)
        Dim NodeSelectionne As XmlElement = CType(XMLBinding.SelectedItem, XmlElement)
        Select Case XMLBinding.SelectedItems.Count
            Case 1
                If wpfMsgBox.MsgBoxQuestion("Mise a jour liste de recherche", "Voulez-vous remplacer la recherche sélectionnée?", Nothing, NodeSelectionne.SelectSingleNode("basic_information/artists/name").InnerText & _
                                            " - " & NodeSelectionne.SelectSingleNode("basic_information/title").InnerText) Then
                    NodeSelectionne.SelectSingleNode("basic_information/id").InnerText = idRelease
                Else
                    CreateIdWantList(idRelease)
                End If
            Case 0
                If wpfMsgBox.MsgBoxQuestion("Ajouter une recherche", "Voulez-vous ajouter une recherche avec le numéro d'ID : " & idRelease, Nothing) Then
                    CreateIdWantList(idRelease)
                End If
            Case Else
        End Select
    End Sub
    Private Sub DataProvider_DataChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles DataProvider.DataChanged
        NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
        If DataProvider.Document IsNot Nothing Then RaiseEvent IdWantlistUpdated(DataProvider.Document, Me)
    End Sub

    Private Sub CreateIdWantList(ByVal newIDRelease As String)
        Dim Newid As String = ""
        If newIDRelease <> "" Then
            Newid = newIDRelease
        Else
            Exit Sub
        End If
        For Each i As XmlElement In XMLBinding.Items
            If i.SelectSingleNode("basic_information/id").InnerText = Newid Then
                XMLBinding.SelectedItem = i
                XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
                Exit Sub
            End If
        Next
        DiscogsServer.RequestAdd_WantListId(Application.Config.user_name, Newid, New DelegateRequestResult(AddressOf DiscogsServerAddIdResultNotify))
    End Sub
    Private Sub DeleteIdWantList()
        Dim NodeSelectionne As XmlElement = CType(XMLBinding.SelectedItem, XmlElement)
        If wpfMsgBox.MsgBoxQuestion("Confirmation suppression",
                                    IIf(XMLBinding.SelectedItems.Count > 1, "Voulez-vous supprimer les recherches sélectionnées?", _
                                                                            "Voulez-vous supprimer la recherche sélectionnée?"), Me, _
                                    IIf(XMLBinding.SelectedItems.Count > 1, XMLBinding.SelectedItems.Count & " recherches sélectionnées", _
                                        NodeSelectionne.SelectSingleNode("basic_information/artists/name").InnerText & _
                                        " - " & NodeSelectionne.SelectSingleNode("basic_information/title").InnerText)) Then
            Dim NodeASelectionner As XmlElement = Nothing
            For Each i As XmlElement In XMLBinding.SelectedItems
                NodeASelectionner = i.NextSibling
                DiscogsServer.RequestDelete_WantListId(Application.Config.user_name, i.SelectSingleNode("basic_information/id").InnerText, New DelegateRequestResult(AddressOf DiscogsServerDeleteIdResultNotify))
            Next
            XMLBinding.SelectedItem = NodeASelectionner
        End If
    End Sub

    '***********************************************************************************************
    '------------------------------GESTION DES BOUTONS DE LA WANTLIST-------------------------------
    '***********************************************************************************************
    Private Sub BPEnregistrer_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        Dim source As String = DataProvider.Source.LocalPath
        DataProvider.Document.Save(source)
    End Sub
    Private Sub BPWantList_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles BPWantList.Click
        Me.IsEnabled = False
        NbreElementsAffiches.Text = "Mise a jour en cours...."
        DiscogsServer.RequestGet_WantListAll(Application.Config.user_name, New DelegateRequestResult(AddressOf DiscogsServerGetAllResultNotify))
    End Sub

    '***********************************************************************************************
    '---------------GESTION DES NOTIFICATIONS DE MISE A JOUR COLLECTION ET WANTLIST-----------------
    '***********************************************************************************************
    Public Function NotifyAddIdWantlist(ByVal newIDRelease As String) As Boolean Implements iNotifyWantListUpdate.NotifyAddIdWantlist
        Dim Newid As String = ""
        If newIDRelease <> "" Then
            Newid = newIDRelease
        Else
            Return False
        End If
        For Each i As XmlElement In DataProvider.Document.DocumentElement.ChildNodes
            If i.SelectSingleNode("basic_information/id").InnerText = Newid Then
                XMLBinding.SelectedItem = i
                XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
                Return True
            End If
        Next
        DiscogsServer.RequestGet_WantListId(Application.Config.user_name, Newid, New DelegateRequestResult(AddressOf DiscogsServerGetIdResultNotify))
        Return True
    End Function
    Public Function NotifyRemoveIdWantlist(ByVal id As String) As Boolean Implements iNotifyWantListUpdate.NotifyRemoveIdWantlist
        Dim IdExistant As Boolean = False
        Dim Item As XmlElement = Nothing
        For Each i As XmlElement In DataProvider.Document.DocumentElement.ChildNodes
            If i.SelectSingleNode("basic_information/id").InnerText = id Then
                Item = i
                IdExistant = True
                Exit For
            End If
        Next
        If IdExistant Then
            XMLBinding.SelectedItem = Item.NextSibling
            Item.ParentNode.RemoveChild(Item)
        End If
        NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
        Return True
    End Function
    Public Function NotifyUpdateWantList(ByVal Document As XmlDocument) As Boolean Implements iNotifyWantListUpdate.NotifyUpdateWantList
    End Function

    '***********************************************************************************************
    '------------------------------DELEGATE POUR REPONSE ACTIONS DISCOGSSERVER----------------------
    '***********************************************************************************************
    Private Sub DiscogsServerGetAllResultNotify(ByVal xmlFileResult As String, ByVal IdRelease As String)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           If xmlFileResult <> "" Then
                                                               DataProvider.Source = Nothing
                                                               DataProvider.Source = New Uri(xmlFileResult)
                                                               'DataProvider.Refresh()
                                                           Else
                                                               wpfMsgBox.MsgBoxInfo("La mise à jour à échouée", "Les informations Discogs n'ont pas pu être mis à jour", Me)
                                                           End If
                                                           NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
                                                           Me.IsEnabled = True
                                                       End Sub))
    End Sub
    Private Sub DiscogsServerAddIdResultNotify(ByVal AddResult As String, ByVal IdRelease As String)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           If AddResult <> "" Then
                                                               Dim IdExistant As Boolean = False
                                                               Dim Item As XmlElement = Nothing
                                                               For Each i As XmlElement In DataProvider.Document.DocumentElement.ChildNodes
                                                                   If i.SelectSingleNode("basic_information/id").InnerText = IdRelease Then
                                                                       XMLBinding.SelectedItem = i
                                                                       XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
                                                                       Exit Sub
                                                                   End If
                                                               Next
                                                               Dim doc As New XmlDocument()
                                                               doc.LoadXml(AddResult)
                                                               Dim newBook As XmlNode = DataProvider.Document.ImportNode(doc.FirstChild(), True)
                                                               DataProvider.Document.SelectSingleNode("WANTLIST").AppendChild(newBook)
                                                               RefreshSort()
                                                               XMLBinding.SelectedItem = newBook
                                                               XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
                                                               NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
                                                               RaiseEvent IdWantlistAdded(IdRelease, Me)
                                                           End If
                                                       End Sub))
    End Sub
    Private Sub DiscogsServerDeleteIdResultNotify(ByVal DeleteResult As String, ByVal IdRelease As String)
        Dim Retour As Boolean = CBool(DeleteResult)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           If Retour Then
                                                               Dim IdExistant As Boolean = False
                                                               Dim Item As XmlElement = Nothing
                                                               For Each i As XmlElement In DataProvider.Document.DocumentElement.ChildNodes
                                                                   If i.SelectSingleNode("basic_information/id").InnerText = IdRelease Then
                                                                       Item = i
                                                                       IdExistant = True
                                                                       Exit For
                                                                   End If
                                                               Next
                                                               If IdExistant Then
                                                                   XMLBinding.SelectedItem = Item.NextSibling
                                                                   Item.ParentNode.RemoveChild(Item)
                                                               End If
                                                               NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
                                                               RaiseEvent IdWantlistRemoved(IdRelease, Me)
                                                           End If
                                                       End Sub))

    End Sub
    Private Sub DiscogsServerGetIdResultNotify(ByVal GetResult As String, ByVal IdRelease As String)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           If GetResult <> "" Then
                                                               Dim doc As New XmlDocument()
                                                               doc.LoadXml(GetResult)
                                                               Dim newBook As XmlNode = DataProvider.Document.ImportNode(doc.FirstChild(), True)
                                                               DataProvider.Document.SelectSingleNode("WANTLIST").AppendChild(newBook)
                                                               RefreshSort()
                                                               XMLBinding.SelectedItem = newBook
                                                               XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
                                                               NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
                                                           End If
                                                       End Sub))
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DES MENUS LA LISTE DES VINYLS-------------------------
    '***********************************************************************************************
    Private Sub XMLBinding_PreviewMouseRightButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles XMLBinding.PreviewMouseRightButtonDown
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
    End Sub
    Private Function CreationMenuContextuelDynamique(ByVal NomChamp As String) As ContextMenu
        If MenuContextuel IsNot Nothing Then
            If MenuContextuel.Tag = NomChamp Then Return MenuContextuel
        End If
        MenuContextuel = New ContextMenu
        MenuContextuel.Tag = NomChamp
        Select Case NomChamp
            Case "General"
                Dim ListeMenu As New List(Of String) 'Libelle menu;Tag envoyé à la fonction de reponse,Nom sous menu
                ListeMenu.Add("Supprimer un vinyl;SupprimerVinyl;supprimervinyl24.png")
                ListeMenu.Add(";;")
                ListeMenu.ForEach(Sub(i As String)
                                      Dim ItemMenu As New MenuItem
                                      Dim TabChaine() As String = Split(i, ";")
                                      If TabChaine(0) <> "" Then
                                          If TabChaine(1) <> "" Then
                                              ItemMenu.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf MenuDynamique_Click))
                                              ItemMenu.Name = TabChaine(1)
                                              ItemMenu.Tag = TabChaine(1)
                                          End If
                                          If TabChaine.Count >= 3 Then
                                              If TabChaine(2) <> "" Then
                                                  Dim ImageIcon As Image = New Image()
                                                  ImageIcon.Height = 16
                                                  ImageIcon.Width = 16
                                                  ImageIcon.Stretch = Stretch.Fill
                                                  ImageIcon.Source = GetBitmapImage("../Images/imgmenus/" & TabChaine(2))
                                                  ItemMenu.Icon = ImageIcon
                                              End If
                                          End If
                                          ItemMenu.Header = TabChaine(0)
                                          MenuContextuel.Items.Add(ItemMenu)
                                      Else
                                          MenuContextuel.Items.Add(New Separator)
                                      End If
                                  End Sub)
        End Select
        Return MenuContextuel
    End Function
    Sub MenuDynamique_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
        Select Case CType(e.OriginalSource, MenuItem).Name
            Case "SupprimerVinyl"
                DeleteIdWantList()
        End Select
    End Sub
    Private Function GetBitmapImage(ByVal NomImage As String) As BitmapImage
        Dim bi3 As New BitmapImage
        bi3.BeginInit()
        bi3.UriSource = New Uri(NomImage, UriKind.RelativeOrAbsolute)
        bi3.EndInit()
        Return bi3
    End Function

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
                    DeleteIdWantList()
                    e.Handled = True
                End If
            Case Key.A To Key.Z
                If TypeOf (e.OriginalSource) Is ListViewItem Or TypeOf (e.OriginalSource) Is ListView Or TypeOf (e.OriginalSource) Is ScrollViewer Then
                    If ColonneTriEnCours Is Nothing Then Exit Select
                    Dim NodeRecherche As String = ""
                    Select Case ColonneTriEnCours.Content.ToString
                        Case "Artiste"
                            NodeRecherche = "basic_information/artists/name"
                        Case "Titre"
                            NodeRecherche = "basic_information/title"
                        Case "Label"
                            NodeRecherche = "basic_information/labels/name"
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
    Private Sub ListeFichiers_PreviewMouseLeftButtonUp(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles XMLBinding.PreviewMouseLeftButtonUp
        StartPoint = New Point
    End Sub
    Private Sub ListeFichiers_PreviewMouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles XMLBinding.PreviewMouseLeftButtonDown
        StartPoint = e.GetPosition(XMLBinding)
        TypeObjetClic = e.OriginalSource
        If (TypeOf (e.OriginalSource) Is ScrollViewer) Then XMLBinding.SelectedItems.Clear()
        If TypeOf (e.OriginalSource) Is Image Then
            If CType(e.OriginalSource, Image).Name Like "tagLink*" Then
                If (Keyboard.GetKeyStates(Key.LeftCtrl) And KeyStates.Down) = 0 Then
                    UpdateRecherche("tagLinkid", CType(CType(e.OriginalSource, Image).Tag, XmlElement).InnerText)
                    e.Handled = True
                Else
                    Dim Newurl As String = ""
                    Newurl = "http://www.discogs.com/release/" & CType(CType(e.OriginalSource, Image).Tag, XmlElement).InnerText
                    Dim NewURI As Uri = New Uri(Newurl)
                    RaiseEvent RequeteWebBrowser(NewURI)
                    e.Handled = True
                End If
            End If
        End If

        If TypeOf (e.OriginalSource) Is TextBlock Then
            If CType(e.OriginalSource, TextBlock).Name Like "tagLink*" Then
                If (Keyboard.GetKeyStates(Key.LeftCtrl) And KeyStates.Down) = 0 Then
                    TexteLink = CType(e.OriginalSource, TextBlock)
                    UpdateRecherche(CType(e.OriginalSource, TextBlock).Name, CType(e.OriginalSource, TextBlock).Text)
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
                        e.Handled = True
                    Catch ex As Exception
                        wpfMsgBox.MsgBoxInfo("Lien URL non valide", "Le lien """ & Newurl & """ est non valide", Nothing)
                    End Try
                End If
            End If
        End If
        '  wpfMsgBox.MsgBoxInfo("INFO", e.OriginalSource.ToString, Nothing)
    End Sub
    Private Sub ListeFichiers_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles XMLBinding.MouseLeave
        StartPoint = New Point
    End Sub
    Dim TexteLink As TextBlock = Nothing
    Private Sub ListeFichiers_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles XMLBinding.MouseMove
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
    Private Sub ListeFichiers_Drop(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles XMLBinding.Drop
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As XmlElement = Nothing
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            DonneeSurvolee = CType(ItemSurvole.Content, XmlElement)
            CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).Opacity = 0
        End If
        If (e.Data.GetDataPresent("tagID3FilesInfosDO")) Then
            FileDrop(e, )
        Else
            e.Effects = DragDropEffects.None
        End If
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
        ElseIf wpfApplication.FindAncestor(e.OriginalSource, "ListView") IsNot Nothing Then
            ElementSurvolePrecedent = Nothing
            TempsDeDebutSurvole = Now.Ticks
            ExPosY = 0
        End If
        If (e.Data.GetDataPresent("tagID3FilesInfosDO")) Then
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
    Private Sub ListeFichiers_DragLeave(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles XMLBinding.DragLeave
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
    Private Sub ListeFichiers_DragOver(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles XMLBinding.DragOver
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
                    Debug.Print(Intervale.ToString)
                    XMLBinding.SelectedItem = DonneeSurvolee
                End If
            Else
                ElementSurvolePrecedent = DonneeSurvolee
                TempsDeDebutSurvole = Now.Ticks
            End If
        End If
        If (e.Data.GetDataPresent("tagID3FilesInfosDO")) Then
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
        If e.Data.GetDataPresent("tagID3FilesInfosDO2") Then
            Dim Info As List(Of tagID3FilesInfosDO) = CType(e.Data.GetData("tagID3FilesInfosDO2"), List(Of tagID3FilesInfosDO))
            For Each i In Info
                CreateIdWantList(i.idRelease)
            Next
        ElseIf e.Data.GetDataPresent("tagID3FilesInfosDO") Then
            Dim Info As tagID3FilesInfosDO = CType(e.Data.GetData("tagID3FilesInfosDO"), tagID3FilesInfosDO)
            CreateIdWantList(Info.idRelease)
        End If
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DES RECHERCHES DANS LISTE-----------------------------
    '***********************************************************************************************
    Private Sub RecherchePrecedente_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles RecherchePrecedente.Click
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

                    'Case "IconOn"
                    '    Select Case ExtraitChaine(Tab(1), "", "\\{")
                    ' Case "FiltreVinylAPayer"
                    '     FiltreVinylAPayer.Opacity = 0.3
                    ' Case "FiltreVinylForSale"
                    '     FiltreVinylForSale.Opacity = 0.3
                    ' Case "FiltreVinylCollection"
                    '     FiltreVinylCollection.Opacity = 0.3
                    ' Case "FiltreVinylWanted"
                    '     FiltreVinylWanted.Opacity = 0.3
                    ' Case "FiltreVinylDiscogs"
                    '     FiltreVinylDiscogs.Opacity = 0.3
                    ' Case "FiltreVinylARecevoir"
                    '     FiltreVinylARecevoir.Opacity = 0.3
                    'End Select
                    ' Case "IconOff"
                    '     Select Case ExtraitChaine(Tab(1), "", "\\{")
                    ' Case "FiltreVinylAPayer"
                    '     FiltreVinylAPayer.Opacity = 1
                    ' Case "FiltreVinylForSale"
                    '     FiltreVinylForSale.Opacity = 1
                    '' Case "FiltreVinylCollection"
                    '    FiltreVinylCollection.Opacity = 1
                    'Case "FiltreVinylWanted"
                    '    FiltreVinylWanted.Opacity = 1
                    'Case "FiltreVinylDiscogs"
                    '    FiltreVinylDiscogs.Opacity = 1
                    'Case "FiltreVinylARecevoir"
                    '    FiltreVinylARecevoir.Opacity = 1
                    'End Select
            End Select
            UpdateFiltre()
            Dim Selection As String = ExtraitChaine(ListeDesRecherches.Last, "\\{", "", 3, True)
            If Selection <> "" Then
                If Left(Selection, 2) = "id" Then
                    Dim RepereSelection As String = ExtraitChaine(Selection, "id:", "", 3, True)
                    For Each i As XmlElement In XMLBinding.Items
                        If i.SelectSingleNode("basic_information/id").InnerText = RepereSelection Then XMLBinding.SelectedItem = i
                    Next
                Else
                    Dim ArtisteSelection As String = ExtraitChaine(Selection, "artiste:", "", 8, True)
                    For Each i As XmlElement In XMLBinding.Items
                        If i.SelectSingleNode("basic_information/artists/name").InnerText = ArtisteSelection Then XMLBinding.SelectedItem = i
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
        'If Not AnnuleRechercheEnCours.IsChecked Then
        ListeDesRecherches.Clear()
        RechercheArtiste.Text = ""
        'FiltreVinylAPayer.Opacity = 0.3
        'FiltreVinylForSale.Opacity = 0.3
        'FiltreVinylCollection.Opacity = 0.3
        'FiltreVinylWanted.Opacity = 0.3
        'FiltreVinylDiscogs.Opacity = 0.3
        'FiltreVinylARecevoir.Opacity = 0.3
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
                    If NodeSelectionne.SelectSingleNode("basic_information/id").InnerText <> "" Then
                        Selection = "id:" & NodeSelectionne.SelectSingleNode("basic_information/id").InnerText
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
                        If NodeSelectionne.SelectSingleNode("basic_information/id").InnerText <> "" Then
                            Selection = "id:" & NodeSelectionne.SelectSingleNode("basic_information/id").InnerText
                        End If
                    End If
                    ListeDesRecherches.Add("Recherche:" & RechercheArtiste.Text & "\\{" & Selection)
                    RecherchePrecedente.IsEnabled = True
                Else
                    ListeDesRecherches.Clear()
                    RecherchePrecedente.IsEnabled = False
                End If
                UpdateFiltre()
            Case Key.LeftCtrl
                IndicateurRechercheDupliquer.IsChecked = Not IndicateurRechercheDupliquer.IsChecked
        End Select
    End Sub
    'PROCEDURES D'APPEL RECHERCHE BIBLIOTHEQUE
    Private Sub UpdateRecherche(ByVal NomLien As String, ByVal TexteLien As String) 'ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        'Dim Lien As TextBlock = CType(e.OriginalSource, TextBlock)
        If TexteLien <> "" Then
            Dim ShiftDown As Boolean = (Keyboard.GetKeyStates(Key.LeftShift) And KeyStates.Down) > 0
            Select Case NomLien
                Case "tagLinkid"
                    If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("id:" & TexteLien, Not ShiftDown)
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("id:" & TexteLien, Not ShiftDown, True)
                Case "tagLinkArtiste"
                    If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("artiste:" & TexteLien, Not ShiftDown)
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("artiste:" & TexteLien, Not ShiftDown, True)
                Case "tagLinkTitre"
                    Dim Chaine As String = Trim(ExtraitChaine(TexteLien, "", "(", False))
                    If Chaine = "" Then Chaine = TexteLien
                    If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("titre:" & Chaine, Not ShiftDown)
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("titre:" & Chaine, Not ShiftDown, True)
                Case "tagLinkLabel"
                    If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("label:" & TexteLien, Not ShiftDown)
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("label:" & TexteLien, Not ShiftDown, True)
                Case "tagLinkCatalogue"
                    If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("catalogue:" & TexteLien, Not ShiftDown)
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("catalogue:" & TexteLien, Not ShiftDown, True)
                Case "tagLinkAnnee"
                    If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("annee:" & TexteLien, Not ShiftDown)
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("annee:" & TexteLien, Not ShiftDown, True)
                Case "tagLinkStyle"
                    If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("style:" & TexteLien, Not ShiftDown)
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("style:" & TexteLien, Not ShiftDown, True)
            End Select
        End If
    End Sub
    Private Sub EnvoieRequeteRecherche(ByVal ChaineRecherche As String, ByVal NewRequete As Boolean)
        FiltreBloque = True
        RaiseEvent RequeteRecherche(ChaineRecherche, NewRequete)
        FiltreBloque = False
    End Sub

    Private Sub UpdateFiltre()
        'PRINCIPE DE CODAGE DE LA REQUETE
        ' syntaxe de la requete 'Nom critere:Valeur cherchee,Autre critere: Valeur cherchee...'
        ' pour une recherche non stricte ajouter '*' en debut du nom du critère
        ' pour une recherche plus stricte ajouter '+' en debut du nom du critère
        ' liste des criteres : id - artiste - titre - label - catalogue - pays - annee - style
        Dim Chaine As String = RechercheArtiste.Text
        XMLBinding.Items.Filter = Function(Ob As Object)
                                      Dim Vinyl As XmlElement = TryCast(Ob, XmlElement)
                                      Dim TabBranche() As String = Split(Chaine, "/")
                                      Dim MemResultatOR As Boolean = False
                                      For Each Branche In TabBranche
                                          Dim Resultat As Boolean = False
                                          If (TabBranche.Count = 1) And (Branche = "") Then MemResultatOR = True
                                          Dim TabCriteres() As String = Split(Branche, ",") ' Split(Chaine, ",")
                                          Dim MemResultatAnd As Boolean = True
                                          For Each j In TabCriteres
                                              Dim NomCritere As String = Trim(ExtraitChaine(j, "", ":"))
                                              Dim ChaineRecherche As String = Trim(ExtraitChaine(j, ":", "", , True))
                                              If j <> "" Then Resultat = True Else Resultat = False
                                              Select Case NomCritere
                                                  Case "id", "+id", "*id"
                                                      If Vinyl.SelectSingleNode("basic_information/id").InnerText <> ChaineRecherche Then Resultat = False
                                                  Case "artiste", "a", "+artiste", "+a", "*artiste", "*a"
                                                      If Left(NomCritere, 1) = "+" Then
                                                          If Vinyl.SelectSingleNode("basic_information/artists/name").InnerText <> ChaineRecherche Then Resultat = False
                                                      ElseIf Left(NomCritere, 1) = "*" Then
                                                          For Each i In Split(ChaineRecherche)
                                                              If InStr(Vinyl.SelectSingleNode("basic_information/artists/name").InnerText, Trim(i), CompareMethod.Text) = 0 Then Resultat = False
                                                          Next
                                                      Else
                                                          If InStr(Vinyl.SelectSingleNode("basic_information/artists/name").InnerText, ChaineRecherche, CompareMethod.Text) = 0 Then Resultat = False
                                                      End If
                                                  Case "titre", "t", "+titre", "+t", "*titre", "*t"
                                                      If Left(NomCritere, 1) = "+" Then
                                                          If Vinyl.SelectSingleNode("basic_information/title").InnerText <> ChaineRecherche Then Resultat = False
                                                      ElseIf Left(NomCritere, 1) = "*" Then
                                                          For Each i In Split(ChaineRecherche)
                                                              If InStr(Vinyl.SelectSingleNode("basic_information/title").InnerText, Trim(i), CompareMethod.Text) = 0 Then Resultat = False
                                                          Next
                                                      Else
                                                          If InStr(Vinyl.SelectSingleNode("basic_information/title").InnerText, ChaineRecherche, CompareMethod.Text) = 0 Then Resultat = False
                                                      End If
                                                  Case "label", "l", "+label", "+l", "*label", "*l"
                                                      If Left(NomCritere, 1) = "+" Then
                                                          If Vinyl.SelectSingleNode("basic_information/labels/name").InnerText <> ChaineRecherche Then Resultat = False
                                                      ElseIf Left(NomCritere, 1) = "*" Then
                                                          For Each i In Split(ChaineRecherche)
                                                              If InStr(Vinyl.SelectSingleNode("basic_information/labels/name").InnerText, Trim(i), CompareMethod.Text) = 0 Then Resultat = False
                                                          Next
                                                      Else
                                                          If (ChaineRecherche = "") Then
                                                              If (Vinyl.SelectSingleNode("basic_information/labels/name").InnerText <> "") Then Resultat = False
                                                          Else
                                                              If InStr(Vinyl.SelectSingleNode("basic_information/labels/name").InnerText, ChaineRecherche, CompareMethod.Text) = 0 Then Resultat = False
                                                          End If
                                                      End If
                                                  Case "catalogue", "c", "+catalogue", "+c", "*catalogue", "*c"
                                                      For Each i In Split(ChaineRecherche)
                                                          If InStr(Vinyl.SelectSingleNode("basic_information/labels/catno").InnerText, Trim(i), CompareMethod.Text) = 0 Then Resultat = False
                                                      Next
                                                  Case "annee", "y", "+annee", "+y", "*annee", "*y"
                                                      If Vinyl.SelectSingleNode("basic_information/year").InnerText <> ChaineRecherche Then Resultat = False
                                                  Case "style", "s", "+style", "+s", "*style", "*s"
                                                  Case "pays", "+pays", "*pays"
                                                  Case Else
                                                      For Each i In Split(ChaineRecherche)
                                                          If InStr(Vinyl.SelectSingleNode("basic_information/artists/name").InnerText, Trim(i), CompareMethod.Text) = 0 Then
                                                              Resultat = False
                                                          End If
                                                      Next
                                              End Select
                                              MemResultatAnd = MemResultatAnd And Resultat
                                          Next
                                          MemResultatOR = MemResultatAnd Or MemResultatOR
                                      Next
                                      Return MemResultatOR
                                  End Function
        NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
    End Sub

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
        XMLBinding.Items.SortDescriptions.Add(New SortDescription("basic_information/artits/name", ListSortDirection.Ascending))
        XMLBinding.Items.SortDescriptions.Add(New SortDescription("basic_information/title", ListSortDirection.Ascending))
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
            XMLBinding.Items.SortDescriptions.Add(New SortDescription("basic_information/artits/name", ListSortDirection.Ascending))
            XMLBinding.Items.SortDescriptions.Add(New SortDescription("basic_information/title", ListSortDirection.Ascending))
        Catch ex As Exception
            ColonneTriEnCours = Nothing
        End Try
    End Sub
    'Procedure de rafraichissement du tri
    Public Sub RefreshSort()
        If ColonneTriEnCours IsNot Nothing Then
            XMLBinding.Items.SortDescriptions.Clear()
            XMLBinding.Items.SortDescriptions.Add(New SortDescription(ColonneTriEnCours.Tag, IconeDeTriEnCours.Direction))
            XMLBinding.Items.SortDescriptions.Add(New SortDescription("basic_information/artits/name", ListSortDirection.Ascending))
            XMLBinding.Items.SortDescriptions.Add(New SortDescription("basic_information/title", ListSortDirection.Ascending))
        End If
    End Sub

End Class
