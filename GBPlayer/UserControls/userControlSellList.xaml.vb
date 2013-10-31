Option Compare Text
Imports System.ComponentModel
Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Threading
Imports System.Windows.Threading
Imports System.Reflection
Imports System.Xml
Imports System.Windows.Controls.Primitives
Imports System.Text
Imports gbdev.DiscogsServer

'Imports Microsoft.VisualBasic.Devices

Public Class UserControlSellList

    Public Event RequeteRecherche(ByVal ChaineRecherche As String, ByVal NewRequete As Boolean)
    Public Event RequeteWebBrowser(ByVal url As Uri)

    Private ListeDesRecherches As List(Of String) = New List(Of String)
    Public Property DisplayValidation As Boolean
    Public ReadOnly Property GetXmlDocument As XmlDocument
        Get
            Return DataProvider.Document
        End Get
    End Property

    Dim MenuContextuel As New ContextMenu
    Dim ConditionDataProvider As XmlDataProvider
    Private PathFichierSellList As String
    Private Const GBAU_NOMDOSSIER_IMAGESVINYLS = "GBDev\GBPlayer\Images\Sell"
    Private Const GBAU_NOMDOSSIER_SELLLIST = "GBDev\GBPlayer\Data\Discogs"
    Private Const GBAU_NOMFICHIER_SELLLIST = "DiscogsSellList"
    Private Delegate Sub NoArgDelegate()

    '***********************************************************************************************
    '---------------------------------INITIALISATION DE LA CLASSE-----------------------------------
    '***********************************************************************************************
    Public Sub New()
        ' Cet appel est requis par le concepteur.
        InitializeComponent()
        LinkFileSellList()
    End Sub
    Private Sub LinkFileSellList()
        Dim RepDest = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\" & GBAU_NOMDOSSIER_SELLLIST
        If Directory.Exists(RepDest) Then
            PathFichierSellList = String.Format("{0}\{1}_{2}.xml", {RepDest, GBAU_NOMFICHIER_SELLLIST, Application.Config.user_name})
        End If
        If PathFichierSellList IsNot Nothing Then DataProvider.Source = New Uri(PathFichierSellList)
    End Sub

    Private Sub UserControlSellList_Initialized(sender As Object, e As EventArgs) Handles Me.Initialized
        ConditionDataProvider = CType(FindResource("ConditionDataProvider"), XmlDataProvider)
        ConditionDataProvider.Source = tagID3FilesInfosDO.GetDataProvider
    End Sub
    Private Sub gbSellList_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Loaded
        If Not DisplayValidation Then Exit Sub
        If DataProvider.Document Is Nothing Then LinkFileSellList()
        Dim SauvegardeSellList As New Dictionary(Of String, GridViewColumn)
        For Each i As GridViewColumn In CType(XMLBinding.View, GridView).Columns
            SauvegardeSellList.Add(CType(i.Header, GridViewColumnHeader).Content.ToString, i)
        Next
        CType(XMLBinding.View, GridView).Columns.Clear()
        Application.Config.sellList_columns.ForEach(Sub(c As ConfigApp.ColumnDescription)
                                                        Try
                                                            Dim NomColonne As String = c.Name
                                                            Dim Dimension As Double = 0
                                                            Try
                                                                Dimension = c.Size
                                                            Catch ex As Exception
                                                                Dimension = Double.NaN
                                                            End Try
                                                            Dim Colonne As GridViewColumn = SauvegardeSellList.Item(NomColonne)
                                                            SauvegardeSellList.Remove(NomColonne)
                                                            Colonne.Width = Dimension
                                                            CType(XMLBinding.View, GridView).Columns.Add(Colonne)
                                                        Catch ex As Exception
                                                        End Try
                                                    End Sub)
        For Each i In SauvegardeSellList
            Dim Colonne As GridViewColumn = i.Value
            CType(XMLBinding.View, GridView).Columns.Add(Colonne)
        Next
        ActiveTri(Application.Config.sellList_sortColumn.Name, Application.Config.sellList_sortColumn.SortDirection)
        If System.Environment.OSVersion.Platform = PlatformID.Win32NT Then
            If System.Environment.OSVersion.Version.Major > 5 Then PlateformVista = True
        End If
        NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
        FenetreParente = CType(Application.Current.MainWindow, MainWindow)
        ProcessUpdate = FenetreParente.ProcessMiseAJour
    End Sub
    Private Sub gbSellList_Unloaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Unloaded
        If Not DisplayValidation Then Exit Sub
        SaveConfiguration()
    End Sub
    Public Sub SaveConfiguration()
        If Not DisplayValidation Then Exit Sub
        ConfigApp.UpdateListeColonnes(Application.Config.sellList_columns, CType(XMLBinding.View, GridView).Columns)
        If ColonneTriEnCours IsNot Nothing Then
            Application.Config.sellList_sortColumn = New ConfigApp.DescriptionTri(ColonneTriEnCours.Tag, IconeDeTriEnCours.Direction)
        End If
        Dim source As String = DataProvider.Source.LocalPath
        If DataProvider.Document IsNot Nothing Then
            '    DataProvider.Document.Save(source)
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
    '***********************************************GESTION UPDATE SELLLIST****************************************
    '**************************************************************************************************************
    Public Sub UpdateIdSellList(ByVal idRelease As String)
        Dim NodeSelectionne As XmlElement = CType(XMLBinding.SelectedItem, XmlElement)
        If wpfMsgBox.MsgBoxQuestion("Ajouter un disque à vendre", "Voulez-vous ajouter un disque à vendre avec le numéro d'ID : " &
                                            idRelease, Nothing) Then
            CreateIdWantList(idRelease)
        End If
    End Sub
    Private Sub DataProvider_DataChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles DataProvider.DataChanged
        NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
        ' If DataProvider.Document IsNot Nothing Then RaiseEvent IdWantlistUpdated(DataProvider.Document, Me)
    End Sub

    Private Sub CreateIdWantList(ByVal newIDRelease As String)
        Dim Newid As String = ""
        If newIDRelease <> "" Then
            Newid = newIDRelease
        Else
            Exit Sub
        End If
        DiscogsServer.RequestAdd_SellListId(Application.Config.user_name, Newid, New DelegateRequestResult(AddressOf DiscogsServerAddIdResultNotify))
    End Sub
    Private Sub DeleteIdSellList()
        Dim NodeSelectionne As XmlElement = CType(XMLBinding.SelectedItem, XmlElement)
        If wpfMsgBox.MsgBoxQuestion("Confirmation suppression",
                                    IIf(XMLBinding.SelectedItems.Count > 1, "Voulez-vous supprimer les disques à vendre sélectionnés?", _
                                                                            "Voulez-vous supprimer le disque à vendre sélectionné?"), Me, _
                                    IIf(XMLBinding.SelectedItems.Count > 1, XMLBinding.SelectedItems.Count & " disques à vendre sélectionnés", _
                                        NodeSelectionne.SelectSingleNode("release/description").InnerText)) Then
            Dim NodeASelectionner As XmlElement = Nothing
            For Each i As XmlElement In XMLBinding.SelectedItems
                NodeASelectionner = i.NextSibling
                DiscogsServer.RequestDelete_SellListId(Application.Config.user_name, i.SelectSingleNode("id").InnerText,
                                                       New DelegateRequestResult(AddressOf DiscogsServerDeleteIdResultNotify))
            Next
            XMLBinding.SelectedItem = NodeASelectionner
        End If
    End Sub
    '***********************************************************************************************
    '---------------------------------MISE A JOUR DES INFORMATIONS DANS LA LISTE-------------------
    '***********************************************************************************************
    Dim MemValuesItemModified As StringTab = New StringTab
    Dim MemItemModified As XmlElement
    Private Sub UpdateInfosIcons(ByVal OriginalSource As Object)
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As XmlElement = Nothing
        Try
            If TypeOf (OriginalSource) Is Image Then
                If TypeOf (XMLBinding.ContainerFromElement(OriginalSource)) Is ListViewItem Then
                    ItemSurvole = CType(XMLBinding.ContainerFromElement(OriginalSource), ListViewItem)
                    DonneeSurvolee = CType(ItemSurvole.Content, XmlElement)
                    Dim NomDonnee As String = ExtraitChaine(CType(OriginalSource, Image).Name, "Tag", "_", 3)
                    Dim IconClic As String = ExtraitChaine(CType(OriginalSource, Image).Name, "_", "")
                    Dim Valeur As String = DonneeSurvolee.SelectSingleNode(NomDonnee).InnerText
                    If (Valeur = "For Sale") Or (Valeur = "Expired") Or (Valeur = "Draft") Then
                        If (Valeur = "For Sale" Or Valeur = "Expired") And (IconClic = "Draft") Then
                            MemItemModified = DonneeSurvolee
                            MemValuesItemModified.Update("status", Valeur)
                            DonneeSurvolee.SelectSingleNode(NomDonnee).InnerText = "Draft"
                        ElseIf (Valeur = "Draft" Or Valeur = "Expired") And (IconClic = "ForSale") Then
                            MemItemModified = DonneeSurvolee
                            MemValuesItemModified.Update("status", Valeur)
                            DonneeSurvolee.SelectSingleNode(NomDonnee).InnerText = "For Sale"
                        ElseIf (IconClic = "Expired") Then
                            MemItemModified = DonneeSurvolee
                            MemValuesItemModified.Update("status", Valeur)
                            DonneeSurvolee.SelectSingleNode(NomDonnee).InnerText = "Expired"
                        ElseIf (IconClic = "Sold") And (Valeur = "For Sale") Then
                            MemItemModified = DonneeSurvolee
                            MemValuesItemModified.Update("status", Valeur)
                            DonneeSurvolee.SelectSingleNode(NomDonnee).InnerText = "Sold"
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
        End Try
    End Sub
    Private Sub Condition_SourceUpdated(sender As Object, e As DataTransferEventArgs)
        Dim list As ComboBox = e.OriginalSource 'CType(e.TargetObject, ComboBox) ' ItemsControl.ItemsControlFromItemContainer(ItemSurvole)
        Dim idFolderSource As String = 0
        Dim idFolderDestination As String = 0
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            Dim ItemSurvole As ListViewItem = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            Dim DonneeSurvolee As XmlElement = CType(ItemSurvole.Content, XmlElement)
            If DonneeSurvolee.SelectSingleNode("status").InnerText <> "Sold" Then
                XMLBinding.SelectedItem = DonneeSurvolee
                If list.SelectedItem IsNot Nothing Then
                    Dim AncienneValeur As String = list.Text
                    MemItemModified = DonneeSurvolee
                    MemValuesItemModified.Update("condition", AncienneValeur)
                End If
            Else
                DonneeSurvolee.SelectSingleNode("condition").InnerText = list.Text
            End If
        End If
    End Sub
    Private Sub ConditionSleeve_SourceUpdated(sender As Object, e As DataTransferEventArgs)
        Dim list As ComboBox = e.OriginalSource 'CType(e.TargetObject, ComboBox) ' ItemsControl.ItemsControlFromItemContainer(ItemSurvole)
        Dim idFolderSource As String = 0
        Dim idFolderDestination As String = 0
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            Dim ItemSurvole As ListViewItem = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            Dim DonneeSurvolee As XmlElement = CType(ItemSurvole.Content, XmlElement)
            If DonneeSurvolee.SelectSingleNode("status").InnerText <> "Sold" Then
                If list.SelectedItem IsNot Nothing Then
                    Dim AncienneValeur As String = list.Text
                    MemItemModified = DonneeSurvolee
                    MemValuesItemModified.Update("sleeve_condition", AncienneValeur)
                End If
            Else
                DonneeSurvolee.SelectSingleNode("sleeve_condition").InnerText = list.Text
            End If
        End If
    End Sub
    Private Sub tagTexte_GotFocus(sender As Object, e As RoutedEventArgs)
        Dim Box As TextBox = e.OriginalSource
        If Box.Name = "tagPrix" Or Box.Name = "tagCommentaire" Then
            If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
                Dim ItemSurvole As ListViewItem = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
                Dim DonneeSurvolee As XmlElement = CType(ItemSurvole.Content, XmlElement)
                Select Case Box.Name
                    Case "tagPrix"
                        MemItemModified = DonneeSurvolee
                        ' MemValuesItemModified.Update("price/value", CType(Box.Tag, String))
                        Box.Tag = DonneeSurvolee.SelectSingleNode("price/value").InnerText
                    Case "tagCommentaire"
                        MemItemModified = DonneeSurvolee
                        ' MemValuesItemModified.Update("comments", CType(Box.Tag, String))
                        Box.Tag = DonneeSurvolee.SelectSingleNode("comments").InnerText
                End Select
            End If
        End If
    End Sub
    Dim ModifEnCours As Boolean
    Private Sub tagTexte_SourceUpdated(sender As Object, e As DataTransferEventArgs)
        Dim Box As TextBox = e.OriginalSource 'CType(e.TargetObject, ComboBox) ' ItemsControl.ItemsControlFromItemContainer(ItemSurvole)
        Dim idFolderSource As String = 0
        Dim idFolderDestination As String = 0
        If (Box.Name = "tagPrix" Or Box.Name = "tagCommentaire") And Not ModifEnCours Then
            ModifEnCours = True
            If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
                Dim ItemSurvole As ListViewItem = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
                Dim DonneeSurvolee As XmlElement = CType(ItemSurvole.Content, XmlElement)
                If DonneeSurvolee.SelectSingleNode("status").InnerText <> "Sold" Then
                    Dim NouvelleValeur As String = Box.Text
                    Select Case Box.Name
                        Case "tagPrix"
                            Dim EchecValidation As Boolean
                            Dim VirguleTrouvee As Boolean
                            For Each i In NouvelleValeur
                                If Not (Char.IsNumber(i) Or ((i = ".") And (Not VirguleTrouvee))) Then
                                    EchecValidation = True
                                    Exit For
                                End If
                                If (i = ".") Then VirguleTrouvee = True
                            Next
                            If Not EchecValidation Then
                                MemItemModified = DonneeSurvolee
                                MemValuesItemModified.Update("price/value", CType(Box.Tag, String))
                            Else
                                DonneeSurvolee.SelectSingleNode("price/value").InnerText = CType(Box.Tag, String)
                                Box.Text = CType(Box.Tag, String)
                            End If

                        Case "tagCommentaire"
                            MemItemModified = DonneeSurvolee
                            MemValuesItemModified.Update("comments", CType(Box.Tag, String))
                    End Select
                Else
                    Select Case Box.Name
                        Case "tagPrix"
                            DonneeSurvolee.SelectSingleNode("price/value").InnerText = CType(Box.Tag, String)
                            Box.Text = CType(Box.Tag, String)
                            Box.Text.ToArray()
                        Case "tagCommentaire"
                            DonneeSurvolee.SelectSingleNode("comments").InnerText = CType(Box.Tag, String)
                            Box.Text = CType(Box.Tag, String)
                    End Select
                End If
            End If
        End If
        ModifEnCours = False
    End Sub
    '***********************************************************************************************
    '---------------------------------GESTION SELECTION--------------------------------------------
    '***********************************************************************************************
    Private Sub XMLBinding_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles XMLBinding.SelectionChanged
        If (TypeOf (e.OriginalSource) Is ListView) And (Not MemValuesItemModified.IsEmpty) Then
            Dim Input As String = "(id=" & MemItemModified.SelectSingleNode("release/id").InnerText & ")" &
                                  "condition=" & MemItemModified.SelectSingleNode("condition").InnerText &
                                  "&sleeve_condition=" & MemItemModified.SelectSingleNode("sleeve_condition").InnerText &
                                  "&price=#F" & MemItemModified.SelectSingleNode("price/value").InnerText &
                                  "&comments=" & MemItemModified.SelectSingleNode("comments").InnerText &
                                  "&status=" & MemItemModified.SelectSingleNode("status").InnerText
            Dim id As String = MemItemModified.SelectSingleNode("id").InnerText & "/" & MemValuesItemModified.ToString
            DiscogsServer.RequestChange_SellListId(Input, id,
                                                  New DelegateRequestResult(AddressOf DiscogsServerChangeIdResultNotify))
            MemItemModified = Nothing
            MemValuesItemModified = New StringTab
        End If
    End Sub

    '***********************************************************************************************
    '------------------------------GESTION DES BOUTONS DE LA WANTLIST-------------------------------
    '***********************************************************************************************
    Private Sub BPEnregistrer_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        Dim source As String = DataProvider.Source.LocalPath
        DataProvider.Document.Save(source)
    End Sub
    Private Sub BPSellList_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles BPSellList.Click
        Me.IsEnabled = False
        NbreElementsAffiches.Text = "Mise a jour en cours...."
        DiscogsServer.RequestGet_SellListAll(Application.Config.user_name, New DelegateRequestResult(AddressOf DiscogsServerGetAllResultNotify))
    End Sub

    '***********************************************************************************************
    '------------------------------DELEGATE POUR REPONSE ACTIONS DISCOGSSERVER----------------------
    '***********************************************************************************************
    Private FenetreParente As MainWindow
    Private ProcessUpdate As WindowUpdateProgress
    Private Delegate Sub UpdateWindowsDelegate(ByVal NomFichier As String)
    Private NumProcess As Long
    Private Sub UpdateWindows(ByVal NomFichier As String)
        If InStr(NomFichier, "#INIT#") Then
            NumProcess = ProcessUpdate.AddNewProcess(CInt(ExtraitChaine(NomFichier, "#INIT#", "", 6)))
        ElseIf NomFichier = "#END#" Then
            ProcessUpdate.StopProcess(NumProcess)
        Else
            ProcessUpdate.UpdateWindows(NomFichier, NumProcess)
        End If
    End Sub
    Private Sub DiscogsServerGetAllResultNotify(ByVal xmlFileResult As String, ByVal IdRelease As String)
        Dim DocXCollection As XDocument = Nothing
        If xmlFileResult <> "" Then
            Dim DocXDiscogs As XDocument = XDocument.Load(xmlFileResult)
            If DataProvider.Document IsNot Nothing Then
                DataProvider.Document.Save(PathFichierSellList)
                DocXCollection = XDocument.Load(PathFichierSellList)
                For Each Disque In From i In DocXCollection.<SELLLIST>.<listings> _
                                    Join j In DocXDiscogs.<SELLLIST>.<listings> _
                                    On CStr(i.<id>.Value) Equals CStr(j.<id>.Value) _
                                    Select j, i Order By i.<id>.Value()
                    If Disque.i.<release>.<extension>.<label>.Value <> "" Then
                        Dim NewElement As XElement = <extension>
                                                         <pays><%= Disque.i.<release>.<extension>.<pays>.Value %></pays>
                                                         <style><%= Disque.i.<release>.<extension>.<style>.Value %></style>
                                                         <label><%= Disque.i.<release>.<extension>.<label>.Value %></label>
                                                     </extension>
                        Disque.j.<release>.First.Add(NewElement)
                    End If
                Next
            End If
            Dim Result = From i In DocXDiscogs.<SELLLIST>.<listings> _
                               Where i.<release>.<extension>.<label>.Value = ""
                                Select i
            FenetreParente.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                          New UpdateWindowsDelegate(AddressOf UpdateWindows), "#INIT#" & CInt(Result.Count))
            For Each Disque In Result
                FenetreParente.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                              New UpdateWindowsDelegate(AddressOf UpdateWindows), CStr(Disque.<release>.<description>.Value))
                If Disque.<release>.<extension>.<label>.Value = "" Then
                    Try
                        Dim RequeteDiscogs As Discogs = New Discogs
                        Dim Infos As DiscogsRelease = RequeteDiscogs.release(Disque.<release>.<id>.Value)
                        Dim NewElement As XElement = <extension>
                                                         <pays><%= Infos.pays %></pays>
                                                         <style><%= Infos.style %></style>
                                                         <label><%= Infos.label.nom %></label>
                                                     </extension>
                        Disque.<release>.First.Add(NewElement)
                    Catch ex As Exception
                        Debug.Print(ex.Message)
                    End Try
                End If
            Next
            FenetreParente.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                          New UpdateWindowsDelegate(AddressOf UpdateWindows), "#END#")
            DocXDiscogs.Save(PathFichierSellList)
        End If
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           If xmlFileResult <> "" Then
                                                               DataProvider.Source = Nothing
                                                               DataProvider.Source = New Uri(PathFichierSellList)
                                                               'DataProvider.Refresh()
                                                           Else
                                                               wpfMsgBox.MsgBoxInfo("La mise à jour à échouée", "Les informations Discogs n'ont pas pu être mis à jour", Me)
                                                           End If
                                                           NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
                                                           Me.IsEnabled = True
                                                       End Sub))
    End Sub
    Private Sub DiscogsServerAddIdResultNotify(ByVal AddResult As String, ByVal IdRelease As String)
        If AddResult <> "" Then
            Dim od As XElement = XElement.Parse(AddResult)
            Dim RequeteDiscogs As Discogs = New Discogs
            Dim Infos As DiscogsRelease = RequeteDiscogs.release(IdRelease)
            Dim NewElement As XElement = <extension>
                                             <pays><%= Infos.pays %></pays>
                                             <style><%= Infos.style %></style>
                                             <label><%= Infos.label.nom %></label>
                                         </extension>
            od.<release>.First.Add(NewElement)
            AddResult = od.ToString
        End If
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                 New NoArgDelegate(Sub()
                                                       If AddResult <> "" And DataProvider.Document IsNot Nothing Then
                                                           Dim doc As New XmlDocument()
                                                           doc.LoadXml(AddResult)
                                                           Dim newBook As XmlNode = DataProvider.Document.ImportNode(doc.FirstChild(), True)
                                                           DataProvider.Document.SelectSingleNode("SELLLIST").AppendChild(newBook)
                                                           RefreshSort()
                                                           XMLBinding.SelectedItem = newBook
                                                           XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
                                                           NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
                                                           '  RaiseEvent IdWantlistAdded(IdRelease, Me)
                                                       End If
                                                   End Sub))
    End Sub
    Private Sub DiscogsServerDeleteIdResultNotify(ByVal DeleteResult As String, ByVal IdListing As String)
        Dim Retour As Boolean = CBool(DeleteResult)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           If Retour Then
                                                               Dim IdExistant As Boolean = False
                                                               Dim Item As XmlElement = Nothing
                                                               For Each i As XmlElement In DataProvider.Document.DocumentElement.ChildNodes
                                                                   If i.SelectSingleNode("id").InnerText = IdListing Then
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
                                                               ' RaiseEvent IdWantlistRemoved(IdRelease, Me)
                                                           End If
                                                       End Sub))

    End Sub
    Private Sub DiscogsServerChangeIdResultNotify(ByVal GetResult As String, ByVal IdListing As String)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           Dim id As String = ExtraitChaine(IdListing, "", "/")
                                                           Dim Element = DataProvider.Document.SelectSingleNode("descendant::listings[id=" & id & "]")
                                                           Dim Extension = Element.SelectSingleNode("release/extension")
                                                           If GetResult <> "" Then
                                                               Dim doc As New XmlDocument()
                                                               doc.LoadXml(GetResult)
                                                               Dim newBook As XmlNode = DataProvider.Document.ImportNode(doc.FirstChild(), True)

                                                               'Dim newExtension As XmlNode = DataProvider.Document.ImportNode(Extension, True)
                                                               If Element IsNot Nothing Then
                                                                   Element.SelectSingleNode("condition").InnerText = newBook.SelectSingleNode("condition").InnerText
                                                                   Element.SelectSingleNode("sleeve_condition").InnerText = newBook.SelectSingleNode("sleeve_condition").InnerText
                                                                   Element.SelectSingleNode("price/value").InnerText = newBook.SelectSingleNode("price/value").InnerText
                                                                   Element.SelectSingleNode("comments").InnerText = newBook.SelectSingleNode("comments").InnerText
                                                                   Element.SelectSingleNode("status").InnerText = newBook.SelectSingleNode("status").InnerText
                                                                   'DataProvider.Document.DocumentElement.RemoveChild(Element)
                                                                   'DataProvider.Document.SelectSingleNode("SELLLIST").AppendChild(newBook)
                                                                   'Dim NewElement = DataProvider.Document.SelectSingleNode("descendant::listings[id=" & id & "]")
                                                                   'If NewElement IsNot Nothing Then
                                                                   '    NewElement.SelectSingleNode("release").AppendChild(newExtension)
                                                                   'End If
                                                               End If
                                                           Else
                                                               Dim ValueSaved As StringTab = New StringTab(ExtraitChaine(IdListing, "/", ""))
                                                               For Each i In ValueSaved.Liste
                                                                   Element.SelectSingleNode(i.Key).InnerText = i.Value
                                                               Next
                                                               RefreshSort()
                                                               XMLBinding.SelectedItem = Element
                                                               XMLBinding.ScrollIntoView(XMLBinding.SelectedItem)
                                                           End If
                                                           NbreElementsAffiches.Text = XMLBinding.Items.Count & " vinyls"
                                                       End Sub))
    End Sub
    Private Sub DiscogsServerGetAllOrdersNotify(ByVal xmlFileResult As String, ByVal IdRelease As String)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                         New NoArgDelegate(Sub()
                                                               MenuContextuel.Tag = ""
                                                               If xmlFileResult = "" Then
                                                                   wpfMsgBox.MsgBoxInfo("Echec update", "La mise a jour des ordres à échouée")
                                                               End If
                                                           End Sub))
    End Sub
    '***********************************************************************************************
    '---------------------------------GESTION DES MENUS LA LISTE DES VINYLS-------------------------
    '***********************************************************************************************
    Dim ItemParentMenuContext As ListViewItem
    Private Sub XMLBinding_PreviewMouseRightButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles XMLBinding.PreviewMouseRightButtonDown
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As XmlElement = Nothing
        XMLBinding.ContextMenu = Nothing
        If TypeOf (XMLBinding.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(XMLBinding.ContainerFromElement(e.OriginalSource), ListViewItem)
            ItemParentMenuContext = ItemSurvole
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
        Dim ListeMenu As New List(Of String) 'Libelle menu;Tag envoyé à la fonction de reponse,Nom sous menu
        Select Case NomChamp
            Case "General"
                ListeMenu.Add("Supprimer un vinyl;SupprimerVinyl;;supprimervinyl24.png")
                ListeMenu.Add(";;")
                ListeMenu.Add("Forcer mise à jour de l'image;UpdateImage;;update24.png")
            Case "DiscogsOrder"
                ListeMenu.Add("Selectionner ordre;;ListeOrders;discogsorder24.png")
                ListeMenu.Add(";;")
                ListeMenu.Add("Mise à jour ordres...;UpdateOrdersList;;update24.png")
        End Select
        ListeMenu.ForEach(Sub(i As String)
                              Dim ItemMenu As New MenuItem
                              Dim TabChaine() As String = Split(i, ";")
                              If TabChaine(0) <> "" Then
                                  If TabChaine(1) <> "" Then
                                      ItemMenu.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf MenuDynamique_Click))
                                      ItemMenu.Name = TabChaine(1)
                                      ItemMenu.Tag = TabChaine(1)
                                  End If
                                  If TabChaine(2) <> "" Then
                                      CreationItemsDynamiques(TabChaine(2), ItemMenu.Items)
                                  End If
                                  If TabChaine.Count >= 3 Then
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
        Return MenuContextuel
    End Function
    Private Sub CreationItemsDynamiques(ByVal NomChamp As String, ByVal ItemsMenu As ItemCollection, Optional ByVal Desactive As Boolean = False)
        Select Case NomChamp
            Case "ListeOrders"
                Dim OrdersFileName = DiscogsServer.FileOrdersList(Application.Config.user_name)
                If File.Exists(OrdersFileName) Then
                    Dim DocXDiscogs As XDocument = XDocument.Load(OrdersFileName)
                    Dim Compteur As Integer
                    Dim Result = From i In DocXDiscogs.<ORDERSLIST>.<orders> _
                                        Select i
                    For Each order In Result
                        If order.<status>.Value <> "Merged" Then
                            Dim ItemMenu As New MenuItem
                            ItemMenu.Header = order.<items>.Count & " Items - [" & order.<status>.Value & "] " & order.<id>.Value & " (" & order.<buyer>.<username>.Value & ")"
                            ItemMenu.Tag = order '"Order:" & order.<id>.Value
                            ItemMenu.Name = "Order" & Compteur 'order.<id>.Value
                            ItemMenu.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf MenuDynamique_Click))
                            Dim ImageIcon As Image = New Image()
                            ImageIcon.Height = 16
                            ImageIcon.Width = 16
                            ImageIcon.Stretch = Stretch.Fill
                            ImageIcon.Source = GetBitmapImage("../Images/imgmenus/discogsorder24.png")
                            ItemMenu.Icon = ImageIcon
                            ItemsMenu.Add(ItemMenu)
                            Compteur += 1
                        End If
                    Next
                End If
        End Select
        Return
    End Sub
    Sub MenuDynamique_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
        If CType(e.OriginalSource, MenuItem).Name Like "Order*" Then
            Dim Order As XElement = CType(CType(e.OriginalSource, MenuItem).Tag, XElement)
            Dim result = From i In Order.<items> _
                                        Select i
            Dim Chaine As String = ""
            For Each item In result
                If Chaine = "" Then
                    Chaine &= "lid:" & item.<id>.Value
                Else
                    Chaine &= "/lid:" & item.<id>.Value
                End If
            Next
            FiltreVinyls(Chaine, True, True)
        Else
            Select Case CType(e.OriginalSource, MenuItem).Name
                Case "SupprimerVinyl"
                    DeleteIdSellList()
                Case "UpdateImage"
                    ' Exit Sub
                    Dim Item As ListViewItem = ItemParentMenuContext
                    If Item IsNot Nothing Then
                        Dim NodeSelectionne As XmlElement = CType(XMLBinding.SelectedItem, XmlElement)
                        If NodeSelectionne.SelectSingleNode("release/id").InnerText <> "" Then
                            Dim MemId = NodeSelectionne.SelectSingleNode("release/id").InnerText()
                            Dim Index = ExtraitChaine(MemId, "-", "", , False)
                            If Index = "" Then Index = "1" Else Index = CStr(CInt(Index) + 1)
                            NodeSelectionne.SelectSingleNode("release/id").InnerText = ExtraitChaine(MemId, "", "-", , True) & "-" & Index
                            Dim ImageUpdate As Controls.Image = CType(wpfApplication.FindChild(Item, "tagLinkImagePochette"), Controls.Image)
                            If ImageUpdate.GetBindingExpression(Controls.Image.SourceProperty) IsNot Nothing Then
                                ImageUpdate.GetBindingExpression(Controls.Image.SourceProperty).UpdateTarget()
                            End If
                        End If
                    End If
                Case "UpdateOrdersList"
                    DiscogsServer.RequestGet_OrderListAll(Application.Config.user_name, New DelegateRequestResult(AddressOf DiscogsServerGetAllOrdersNotify))
            End Select
        End If
    End Sub
    Private Function GetBitmapImage(ByVal NomImage As String) As BitmapImage
        Dim bi3 As New BitmapImage
        bi3.BeginInit()
        bi3.UriSource = New Uri(NomImage, UriKind.RelativeOrAbsolute)
        bi3.EndInit()
        Return bi3
    End Function
    Private Sub RechercheArtiste_PreviewMouseRightButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles RechercheArtiste.PreviewMouseRightButtonDown
        'ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Artiste")
        RechercheArtiste.ContextMenu = CreationMenuContextuelDynamique("DiscogsOrder")
        e.Handled = True
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


    '***********************************************************************************************
    '---------------------------------GESTION CLAVIER-----------------------------------------------
    '***********************************************************************************************
    '**************************************************************************************************************
    '**************************************GESTION DU CLAVIER******************************************************
    '**************************************************************************************************************
    Private Sub Me_PreviewKeyDown(ByVal sender As Object, ByVal e As System.Windows.Input.KeyEventArgs) Handles Me.PreviewKeyDown
        Select Case e.Key
            Case Key.Delete
                If TypeOf (e.OriginalSource) Is ListViewItem Then
                    DeleteIdSellList()
                    e.Handled = True
                End If
            Case Key.A To Key.Z
                If TypeOf (e.OriginalSource) Is ListViewItem Or TypeOf (e.OriginalSource) Is ListView Then
                    If ColonneTriEnCours Is Nothing Then Exit Select
                    Dim NodeRecherche As String = ""
                    Select Case ColonneTriEnCours.Content.ToString
                        Case "Description"
                            NodeRecherche = "release/description"
                        Case "Label"
                            NodeRecherche = "release/extension/label"
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
        UpdateInfosIcons(e.OriginalSource)
        If TypeOf (e.OriginalSource) Is Image Then
            If CType(e.OriginalSource, Image).Name Like "tagLink*" Then
                Dim Chaine As String = ExtraitChaine(CType(CType(e.OriginalSource, Image).Tag, XmlElement).InnerText, "", "-", , True)
                If (Keyboard.GetKeyStates(Key.LeftCtrl) And KeyStates.Down) = 0 Then
                    UpdateRecherche("tagLinkid", Chaine)
                    e.Handled = True
                Else
                    Dim Newurl As String = ""
                    Newurl = "http://www.discogs.com/release/" & Chaine
                    Dim NewURI As Uri = New Uri(Newurl)
                    RaiseEvent RequeteWebBrowser(NewURI)
                    e.Handled = True
                End If
            ElseIf CType(e.OriginalSource, Image).Name Like "Drapeau" Then
                If (Keyboard.GetKeyStates(Key.LeftCtrl) And KeyStates.Down) = 0 Then
                    UpdateRecherche("tagLinkPays", CType(CType(e.OriginalSource, Image).Tag, XmlElement).InnerText)
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
    '---------------------------------GESTION DES MISES A JOUR DE LA LISTE DES VINYLS---------------
    '***********************************************************************************************
    Private Sub Filtres_MouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) _
                            Handles FiltreStatusDraft.MouseLeftButtonDown,
                                    FiltreStatusForSale.MouseLeftButtonDown,
                                    FiltreStatusSold.MouseLeftButtonDown,
                                    FiltreStatusExpired.MouseLeftButtonDown
        '   If TempLockLink Then Exit Sub
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

                Case "IconOn"
                    Select Case ExtraitChaine(Tab(1), "", "\\{")
                        Case "FiltreStatusDraft"
                            FiltreStatusDraft.Opacity = 0.3
                        Case "FiltreStatusForSale"
                            FiltreStatusForSale.Opacity = 0.3
                        Case "FiltreStatusSold"
                            FiltreStatusSold.Opacity = 0.3
                        Case "FiltreStatusExpired"
                            FiltreStatusExpired.Opacity = 0.3
                    End Select
                Case "IconOff"
                    Select Case ExtraitChaine(Tab(1), "", "\\{")
                        Case "FiltreStatusDraft"
                            FiltreStatusDraft.Opacity = 1
                        Case "FiltreStatusForSale"
                            FiltreStatusForSale.Opacity = 1
                        Case "FiltreStatusSold"
                            FiltreStatusSold.Opacity = 1
                        Case "FiltreStatusExpired"
                            FiltreStatusExpired.Opacity = 1
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
        FiltreStatusDraft.Opacity = 0.3
        FiltreStatusForSale.Opacity = 0.3
        FiltreStatusSold.Opacity = 0.3
        FiltreStatusExpired.Opacity = 0.3
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
                    If NodeSelectionne.SelectSingleNode("id").InnerText <> "" Then
                        Selection = "id:" & NodeSelectionne.SelectSingleNode("id").InnerText
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
                        If NodeSelectionne.SelectSingleNode("id").InnerText <> "" Then
                            Selection = "id:" & NodeSelectionne.SelectSingleNode("id").InnerText
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
            TexteLien = RemplaceOccurences(TexteLien, ",", " ")
            Select Case NomLien
                Case "tagLinkArtiste"
                    If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("artiste:" & TexteLien, Not ShiftDown)
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("" & TexteLien, Not ShiftDown, True)
                Case "tagLinkTitre"
                    Dim Chaine As String = Trim(ExtraitChaine(TexteLien, "", "(", False))
                    If Chaine = "" Then Chaine = TexteLien
                    If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("titre:" & Chaine, Not ShiftDown)
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("" & Chaine, Not ShiftDown, True)
                Case "tagLinkId"
                    If (IndicateurRechercheDupliquer.IsChecked) Or (Not RechercheLocale.IsChecked) Then EnvoieRequeteRecherche("id:" & TexteLien, Not ShiftDown)
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("id:" & TexteLien, Not ShiftDown, True)
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
                Case "tagLinkPays"
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("state:" & TexteLien, Not ShiftDown, True)
                Case "tagLinkCommentaire"
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("comment:" & TexteLien, Not ShiftDown, True)
                Case "tagLinkStatus"
                    If (IndicateurRechercheDupliquer.IsChecked) Or (RechercheLocale.IsChecked) Then FiltreVinyls("status:" & TexteLien, Not ShiftDown, True)
            End Select
        End If
    End Sub
    Private Sub EnvoieRequeteRecherche(ByVal ChaineRecherche As String, ByVal NewRequete As Boolean)
        FiltreBloque = True
        RaiseEvent RequeteRecherche(ChaineRecherche, NewRequete)
        FiltreBloque = False
    End Sub
    Dim PriceSelection As Double
    Private Sub UpdateFiltre()
        'PRINCIPE DE CODAGE DE LA REQUETE
        ' syntaxe de la requete 'Nom critere:Valeur cherchee,Autre critere: Valeur cherchee...'
        ' pour une recherche non stricte ajouter '*' en debut du nom du critère
        ' pour une recherche plus stricte ajouter '+' en debut du nom du critère
        ' liste des criteres : id - artiste - titre - label - catalogue - state - annee - style
        Dim Chaine As String = RechercheArtiste.Text
        PriceSelection = 0
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
                                                  Case "lid"
                                                      If Vinyl.SelectSingleNode("id").InnerText <> ChaineRecherche Then Resultat = False
                                                  Case "id", "+id", "*id"
                                                      If ExtraitChaine(Vinyl.SelectSingleNode("release/id").InnerText, "", "-", , True) <> ChaineRecherche Then Resultat = False
                                                  Case "artiste", "a", "+artiste", "+a", "*artiste", "*a"
                                                      If Left(NomCritere, 1) = "+" Then
                                                          If Vinyl.SelectSingleNode("release/description").InnerText <> ChaineRecherche Then Resultat = False
                                                      ElseIf Left(NomCritere, 1) = "*" Then
                                                          For Each i In Split(ChaineRecherche)
                                                              If InStr(Vinyl.SelectSingleNode("release/description").InnerText, Trim(i), CompareMethod.Text) = 0 Then Resultat = False
                                                          Next
                                                      Else
                                                          If InStr(Vinyl.SelectSingleNode("release/description").InnerText, ChaineRecherche, CompareMethod.Text) = 0 Then Resultat = False
                                                      End If
                                                  Case "titre", "t", "+titre", "+t", "*titre", "*t"
                                                      If Left(NomCritere, 1) = "+" Then
                                                          If Vinyl.SelectSingleNode("release/description").InnerText <> ChaineRecherche Then Resultat = False
                                                      ElseIf Left(NomCritere, 1) = "*" Then
                                                          For Each i In Split(ChaineRecherche)
                                                              If InStr(Vinyl.SelectSingleNode("release/description").InnerText, Trim(i), CompareMethod.Text) = 0 Then Resultat = False
                                                          Next
                                                      Else
                                                          If InStr(Vinyl.SelectSingleNode("release/description").InnerText, ChaineRecherche, CompareMethod.Text) = 0 Then Resultat = False
                                                      End If
                                                  Case "label", "l", "+label", "+l", "*label", "*l"
                                                      If Vinyl.SelectSingleNode("release/extension/label") IsNot Nothing Then
                                                          If Left(NomCritere, 1) = "+" Then
                                                              If Vinyl.SelectSingleNode("release/extension/label").InnerText <> ChaineRecherche Then Resultat = False
                                                          ElseIf Left(NomCritere, 1) = "*" Then
                                                              For Each i In Split(ChaineRecherche)
                                                                  If InStr(Vinyl.SelectSingleNode("release/extension/label").InnerText, Trim(i), CompareMethod.Text) = 0 Then Resultat = False
                                                              Next
                                                          Else
                                                              If (ChaineRecherche = "") Then
                                                                  If (Vinyl.SelectSingleNode("release/extension/label").InnerText <> "") Then Resultat = False
                                                              Else
                                                                  If InStr(Vinyl.SelectSingleNode("release/extension/label").InnerText, ChaineRecherche, CompareMethod.Text) = 0 Then Resultat = False
                                                              End If
                                                          End If
                                                      End If
                                                  Case "catalogue", "c", "+catalogue", "+c", "*catalogue", "*c"
                                                      For Each i In Split(ChaineRecherche)
                                                          If InStr(Vinyl.SelectSingleNode("release/catalog_number").InnerText, Trim(i), CompareMethod.Text) = 0 Then Resultat = False
                                                      Next
                                                  Case "annee", "y", "+annee", "+y", "*annee", "*y"
                                                      If Vinyl.SelectSingleNode("release/year").InnerText <> ChaineRecherche Then Resultat = False
                                                  Case "style", "s", "+style", "+s", "*style", "*s"
                                                      If Vinyl.SelectSingleNode("release/extension/style") IsNot Nothing Then
                                                          If Vinyl.SelectSingleNode("release/extension/style").InnerText <> ChaineRecherche Then Resultat = False
                                                      End If
                                                  Case "state", "+state", "*state"
                                                      If Vinyl.SelectSingleNode("release/extension/pays") IsNot Nothing Then
                                                          If Vinyl.SelectSingleNode("release/extension/pays").InnerText <> ChaineRecherche Then Resultat = False
                                                      End If
                                                  Case "comment"
                                                      If Vinyl.SelectSingleNode("comments") IsNot Nothing Then
                                                          For Each i In Split(ChaineRecherche)
                                                              If InStr(Vinyl.SelectSingleNode("comments").InnerText, i, CompareMethod.Text) = 0 Then Resultat = False
                                                          Next i
                                                      End If
                                                  Case Else
                                                      For Each i In Split(ChaineRecherche)
                                                          If InStr(Vinyl.SelectSingleNode("release/description").InnerText, Trim(i), CompareMethod.Text) = 0 Then
                                                              Resultat = False
                                                          End If
                                                      Next
                                              End Select
                                              MemResultatAnd = MemResultatAnd And Resultat
                                          Next
                                          MemResultatOR = MemResultatAnd Or MemResultatOR
                                      Next
                                      Dim IconResultat As Boolean = True
                                      If (FiltreStatusDraft.Opacity = 1) And (Vinyl.SelectSingleNode("status").InnerText <> "Draft") Then IconResultat = False
                                      If (FiltreStatusForSale.Opacity = 1) And (Vinyl.SelectSingleNode("status").InnerText <> "For Sale") Then IconResultat = False
                                      If (FiltreStatusSold.Opacity = 1) And (Vinyl.SelectSingleNode("status").InnerText <> "Sold") Then IconResultat = False
                                      If (FiltreStatusExpired.Opacity = 1) And (Vinyl.SelectSingleNode("status").InnerText <> "Expired") Then IconResultat = False
                                      If Vinyl.SelectSingleNode("price/value").InnerText <> "" Then
                                          If (MemResultatOR And IconResultat) Then PriceSelection += CDbl(RemplaceOccurences(Vinyl.SelectSingleNode("price/value").InnerText, ".", ","))
                                      End If
                                      Return (MemResultatOR And IconResultat)
                                  End Function
        NbreElementsAffiches.Text = PriceSelection & " EUR - " & XMLBinding.Items.Count & " vinyls"
    End Sub

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
        XMLBinding.Items.SortDescriptions.Add(New SortDescription("release/description", ListSortDirection.Ascending))
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
            XMLBinding.Items.SortDescriptions.Add(New SortDescription("release/description", ListSortDirection.Ascending))
        Catch ex As Exception
            ColonneTriEnCours = Nothing
        End Try
    End Sub
    'Procedure de rafraichissement du tri
    Public Sub RefreshSort()
        If ColonneTriEnCours IsNot Nothing Then
            XMLBinding.Items.SortDescriptions.Clear()
            XMLBinding.Items.SortDescriptions.Add(New SortDescription(ColonneTriEnCours.Tag, IconeDeTriEnCours.Direction))
            XMLBinding.Items.SortDescriptions.Add(New SortDescription("release/description", ListSortDirection.Ascending))
        End If
    End Sub


End Class
