Option Compare Text
Imports System.ComponentModel
Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Threading
Imports System.Windows.Threading

Public Class userControlTagListView
    Implements iNotifyShellUpdate

    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES EVENEMENTS
    Public Event RequeteWebBrowser(ByVal url As Uri)
    Public Event RequeteRecherche(ByVal ChaineRecherche As String, ByVal NewRequete As Boolean)
    Public Event SelectionChanged(ByVal ListeFichiersSel As List(Of String))
    Public Event SelectionDblClick(ByVal NomFichier As String, ByVal Tag As String)
    Public Event RequeteSelectionRepertoire(ByVal Repertoire As String)
    Public Event RequeteConvertFile(ByVal TabFichiers() As String, ByVal Destination As String, ByVal typeConversion As fileConverter.ConvertType)
    Public Event UpdateDebut()
    Public Event UpdateEnCours()
    Public Event UpdateFin()

    Dim RepertoireParDefaut As String
    Dim MiseAJourBloquee As Boolean = False
    ' Dim BackUpListeEnCours As IEnumerable(Of XElement)
    Dim BackUpRechercheEnCours As String
    Dim WithEvents ShellWatcherMp3 As New System.IO.FileSystemWatcher
    Dim WithEvents ShellWatcherWav As New System.IO.FileSystemWatcher
    Private _FilesCollection As New ObservableCollection(Of tagID3FilesInfosDO)
    Public Property DisplayValidation As Boolean
    Public ReadOnly Property FilesCollection As ObservableCollection(Of tagID3FilesInfosDO)
        Get
            Return _FilesCollection
        End Get
    End Property
    Public Property LectureFichier As Boolean
    Public Property SelectedItems As List(Of String)
    Public Property SelectedItem As String
        Get
            If SelectedItems Is Nothing Then Return ""
            If SelectedItems.Count > 0 Then Return SelectedItems.First Else Return ""
        End Get
        Set(ByVal value As String)
            If value <> "" Then
                For Each i In ListeFichiers.Items
                    If CType(i, tagID3FilesInfosDO).NomComplet = value Then
                        ListeFichiers.SelectedItem = i
                        Debug.Print("affectation item")
                        Exit For
                    End If
                Next
            Else
                ListeFichiers.SelectedItem = Nothing
            End If
        End Set
    End Property
    Public Property BibliothequeLiee As tagID3Bibliotheque Implements iNotifyShellUpdate.BibliothequeLiee
    Public Property Filter As String Implements iNotifyShellUpdate.Filter
    'PROPRIETE DE PERSONNALISATION COMPORTEMENT
    Public Property DisableCustomisation As Boolean
    Public Property ForceCopyFiles As Boolean
    Public Property ExecuteFileDropAction As DelegateSpecificDropAction
    Public Property ExecuteDirectoryDropAction As DelegateSpecificDropAction
    Public Delegate Sub DelegateSpecificDropAction(ByVal Name As String)

    Private Delegate Sub NoArgDelegate()
    '**************************************************************************************************************
    '************************************INITIALISATION et FINALISATION DE LA LISTE********************************
    '**************************************************************************************************************
    'PROCEDURE DE CONFIGURATION DE LA LISTE DE FICHIERS
    Private Sub ListeFichiers_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles ListeFichiers.Loaded
        If Not DisplayValidation Then Exit Sub
        If Not DisableCustomisation Then
            Dim ConfigUtilisateur As ConfigPerso = New ConfigPerso
            ConfigUtilisateur = ConfigPerso.LoadConfig
            Dim SauvegardeCollection As New Dictionary(Of String, GridViewColumn)
            For Each i As GridViewColumn In CType(ListeFichiers.View, GridView).Columns
                SauvegardeCollection.Add(CType(i.Header, GridViewColumnHeader).Tag.ToString, i)
            Next
            CType(ListeFichiers.View, GridView).Columns.Clear()
            ConfigUtilisateur.LISTEFICHIERSMP3_ListeColonnes.ForEach(Sub(c As String)
                                                                         Try
                                                                             Dim NomColonne As String = ExtraitChaine(c, "", ";")
                                                                             Dim Position As Long = CLng(ExtraitChaine(c, ";", "/"))
                                                                             Dim Dimension As Double = CDbl(ExtraitChaine(c, "/", ""))
                                                                             Dim Colonne As GridViewColumn = SauvegardeCollection.Item(NomColonne)
                                                                             SauvegardeCollection.Remove(NomColonne)
                                                                             Colonne.Width = Dimension
                                                                             CType(ListeFichiers.View, GridView).Columns.Add(Colonne)
                                                                         Catch ex As Exception
                                                                             MsgBox("erreu")
                                                                         End Try
                                                                     End Sub)
            If SauvegardeCollection.Count > 0 Then
                For Each i In SauvegardeCollection
                    CType(ListeFichiers.View, GridView).Columns.Add(i.Value)
                Next
            End If
            ActiveTri(ExtraitChaine(ConfigUtilisateur.LISTEFICHIERSMP3_ColonneTriee, "", ";"),
                    IIf(ExtraitChaine(ConfigUtilisateur.LISTEFICHIERSMP3_ColonneTriee, ";", "") = "A",
                         ListSortDirection.Ascending, ListSortDirection.Descending))
        End If
        If System.Environment.OSVersion.Platform = PlatformID.Win32NT Then
            If System.Environment.OSVersion.Version.Major > 5 Then PlateformVista = True
        End If
        Me.AddHandler(ContextMenuOpeningEvent, New ContextMenuEventHandler(AddressOf MenuContextuel_Opened))
        Me.AddHandler(ContextMenuClosingEvent, New ContextMenuEventHandler(AddressOf MenuContextuel_Closed))
    End Sub
    'PROCEDURE DE SAUVEGARDE DE LA LISTE DE FICHIERS
    Private Sub ListeFichiers_IsVisibleChanged(ByVal sender As Object, ByVal e As System.Windows.DependencyPropertyChangedEventArgs) Handles ListeFichiers.IsVisibleChanged
        'If Not e.NewValue Then
        ' Dim ConfigUtilisateur As ConfigPerso = New ConfigPerso
        ' ConfigUtilisateur = ConfigPerso.LoadConfig
        ' SaveConfiguration(ConfigUtilisateur)
        ' ConfigPerso.SaveConfig(ConfigUtilisateur)
        ' End If
    End Sub
    Public Sub SaveConfiguration(ByVal ConfigUtilisateur As gbDev.ConfigPerso)
        If DisableCustomisation Then Exit Sub
        ConfigPerso.UpdateListeColonnesTag(ConfigUtilisateur.LISTEFICHIERSMP3_ListeColonnes, CType(ListeFichiers.View, GridView).Columns)
        If ColonneTriEnCours IsNot Nothing Then
            ConfigUtilisateur.LISTEFICHIERSMP3_ColonneTriee = ColonneTriEnCours.Tag & "[" & NomChampTriEnCours & "];" &
                                IIf(IconeDeTriEnCours.Direction = ListSortDirection.Ascending, "A", "D")
        End If
    End Sub

    '**************************************************************************************************************
    '*****************GESTION ASYNCHRONE DE LA MISE A JOUR DES INFORMATIONS DES FICHIERS***************************
    '**************************************************************************************************************
    Private Enum FilesInfosUpdateOrder
        update = 1
        debut = 4
        fin = 5
    End Enum
    Private Delegate Sub ReadListeRepDelegate(ByVal Path As String, ByVal NumMiseAJour As Long, ByVal ItemSelected As String)
    Private Delegate Sub ReadListeXMLDelegate(ByVal Liste As IEnumerable(Of String), ByVal NumMiseAJour As Long)
    Private Delegate Sub ReadListeRechercheDelegate(ByVal Recherche As String, ByVal NumMiseAJour As Long, ByVal ItemSelected As String, ByVal PasRepEnCours As Boolean)
    Private Delegate Sub UpdateListeRepInterfaceDelegate(ByVal arg As tagID3FilesInfos, ByVal Ordre As FilesInfosUpdateOrder,
                                                         ByVal PasRepEnCours As Boolean, ByVal NumMaJ As Long, ByVal ItemSelected As String)
    Private RepertoireAMettreAJour As String
    Dim DemandeMaJ As Long
    Dim MiseAJourEnCours As Boolean
    Dim RequeteXElement As Boolean
    Public Sub MiseAJourListeRepertoire(ByVal Path As String, Optional ByVal ItemSelected As String = "")
        If MiseAJourBloquee Then Exit Sub
        RequeteXElement = False
        DemandeMaJ += 1
        Filter = "dir:" & Path
        RepertoireParDefaut = Path
        _FilesCollection.Clear()
        If Path <> "" Then
            RepertoireAMettreAJour = Path
            Dim ReadTag As New ReadListeRepDelegate(AddressOf ReadListeRep)
            ReadTag.BeginInvoke(RepertoireAMettreAJour, DemandeMaJ, ItemSelected, Nothing, Nothing)
        End If
        Exit Sub
    End Sub
    Sub ReadListeRep(ByVal Path As String, ByVal NumMiseAJour As Long, ByVal ItemSelected As String)
        If DemandeMaJ <> NumMiseAJour Then Exit Sub
        Dim PriorityForDispatcher As DispatcherPriority = System.Windows.Threading.DispatcherPriority.Input
        Try
            ListeFichiers.Dispatcher.BeginInvoke(New UpdateListeRepInterfaceDelegate(AddressOf UpdateListeRepInterface),
                                                 PriorityForDispatcher,
                                                 {Nothing, FilesInfosUpdateOrder.debut, False, NumMiseAJour, ""})
            Dim FinBoucle As Boolean = False
            'MISE A JOUR DU LISTING DES FICHIERS
            'Dim DirList() As String = Directory.GetFiles(Path, "*.mp3")
            Dim DirList2 As IEnumerable(Of String) = From file In Directory.EnumerateFiles(Path, "*.*")
                                Where (file.ToLower.Contains(".mp3")) Or (file.ToLower.Contains(".wav"))
            'INSERER LE TRI A CE NIVEAU
            ' If DirList.Count > 0 Then
            If DirList2 IsNot Nothing Then
                Dim Compteur As Integer
                Array.ForEach(DirList2.ToArray, Sub(i As String)
                                                    If DemandeMaJ = NumMiseAJour Then
                                                        Dim InfoFichierEnCours As tagID3FilesInfos = New tagID3FilesInfos(i)
                                                        If Compteur > 10 Then PriorityForDispatcher = DispatcherPriority.ContextIdle
                                                        ListeFichiers.Dispatcher.BeginInvoke(New UpdateListeRepInterfaceDelegate(AddressOf UpdateListeRepInterface),
                                                                                    PriorityForDispatcher,
                                                                                    {InfoFichierEnCours, FilesInfosUpdateOrder.update, False, NumMiseAJour, ""})
                                                    Else
                                                        Exit Sub
                                                    End If
                                                    Compteur += 1
                                                End Sub)
            End If
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
        ListeFichiers.Dispatcher.BeginInvoke(New UpdateListeRepInterfaceDelegate(AddressOf UpdateListeRepInterface),
                                              PriorityForDispatcher,
                                              {Nothing, FilesInfosUpdateOrder.fin, False, NumMiseAJour, ItemSelected})
    End Sub

    Public Sub MiseAJourListeFichierXml(ByVal Liste As IEnumerable(Of String))
        If Liste IsNot Nothing Then
            DemandeMaJ += 1
            RepertoireParDefaut = ""
            If Directory.Exists(CType(Application.Current.MainWindow, MainWindow).ListeRepertoires.gbRacine) Then
                RepertoireAMettreAJour = CType(Application.Current.MainWindow, MainWindow).ListeRepertoires.gbRacine
            Else
                RepertoireAMettreAJour = ""
            End If
            _FilesCollection.Clear()
            If Liste.Count > 0 Then
                Dim ReadTag As New ReadListeXMLDelegate(AddressOf ReadListeFichierXml)
                ReadTag.BeginInvoke(Liste, DemandeMaJ, Nothing, Nothing)
            End If
        End If
    End Sub
    Sub ReadListeFichierXml(ByVal Liste As IEnumerable(Of String), ByVal NumMiseAJour As Long)
        Try
            ListeFichiers.Dispatcher.BeginInvoke(New UpdateListeRepInterfaceDelegate(AddressOf UpdateListeRepInterface),
                                                 System.Windows.Threading.DispatcherPriority.Normal,
                                                 {Nothing, FilesInfosUpdateOrder.debut, True, NumMiseAJour, ""})
            For Each i In Liste
                Dim InfoFichierEnCours As tagID3FilesInfos
                If DemandeMaJ = NumMiseAJour Then
                    InfoFichierEnCours = New tagID3FilesInfos(i)
                    ListeFichiers.Dispatcher.BeginInvoke(New UpdateListeRepInterfaceDelegate(AddressOf UpdateListeRepInterface),
                                                 System.Windows.Threading.DispatcherPriority.ContextIdle,
                                                 {InfoFichierEnCours, FilesInfosUpdateOrder.update, True, NumMiseAJour, ""})
                End If
            Next
        Catch ex As Exception
        End Try
        ListeFichiers.Dispatcher.BeginInvoke(New UpdateListeRepInterfaceDelegate(AddressOf UpdateListeRepInterface),
                                             System.Windows.Threading.DispatcherPriority.ContextIdle,
                                             {Nothing, FilesInfosUpdateOrder.fin, True, NumMiseAJour, ""})
    End Sub

    Public Sub MiseAJourListeFichierXElement(ByVal ChaineRecherche As String,
                                             Optional ByVal ItemSelected As String = "",
                                             Optional ByVal RepSelectionne As String = "")
        If TempLockLink Then Exit Sub
        RequeteXElement = True
        Dim PasRepEnCours As Boolean = True
        If MiseAJourBloquee And (RepSelectionne <> "") Then Exit Sub
        BackUpRechercheEnCours = ChaineRecherche
        DemandeMaJ += 1
        Filter = ChaineRecherche
        RepertoireParDefaut = RepSelectionne
        If RepSelectionne = "" And Directory.Exists(CType(Application.Current.MainWindow, MainWindow).ListeRepertoires.gbRacine) Then
            RepertoireAMettreAJour = CType(Application.Current.MainWindow, MainWindow).ListeRepertoires.gbRacine
        Else
            RepertoireAMettreAJour = RepSelectionne
            PasRepEnCours = False
        End If
        Dim ReadTag As New ReadListeRechercheDelegate(AddressOf ReadListeFichierXElement)
        ReadTag.BeginInvoke(ChaineRecherche, DemandeMaJ, ItemSelected, PasRepEnCours, Nothing, Nothing)
    End Sub
    ' Sub ReadListeFichierXElement(ByVal Liste As IEnumerable(Of XElement), ByVal NumMiseAJour As Long, ByVal ItemSelected As String, ByVal PasRepEnCours As Boolean)
    Sub ReadListeFichierXElement(ByVal ChaineRecherche As String, ByVal NumMiseAJour As Long, ByVal ItemSelected As String, ByVal PasRepEnCours As Boolean)
        If DemandeMaJ <> NumMiseAJour Then Exit Sub
        If BibliothequeLiee Is Nothing Then Exit Sub
        Dim PriorityForDispatcher As DispatcherPriority = DispatcherPriority.Input
        Try
            Dim ListeTriee As IEnumerable(Of XElement) = TriListeFichiers(BibliothequeLiee.SearchFilesXml(ChaineRecherche)) ' TriListeFichiers(Liste)
            ListeFichiers.Dispatcher.BeginInvoke(New UpdateListeRepInterfaceDelegate(AddressOf UpdateListeRepInterface),
                                                    PriorityForDispatcher,
                                                    {Nothing, FilesInfosUpdateOrder.debut, PasRepEnCours, NumMiseAJour, ""})
            If ListeTriee.Count > 0 Then
                If DemandeMaJ <> NumMiseAJour Then Exit Sub
                Dim Compteur As Integer
                For Each i In ListeTriee
                    Dim ItemASelectionner As String = ""
                    Dim InfoFichierEnCours As tagID3FilesInfos
                    If DemandeMaJ = NumMiseAJour Then
                        InfoFichierEnCours = New tagID3FilesInfos(i) 'New tagID3FilesInfos(i.<Nom>.Value) '
                        If i.<Nom>.Value = ItemSelected Then ItemASelectionner = ItemSelected
                        If Compteur > 2 Then PriorityForDispatcher = DispatcherPriority.Input
                        If Compteur > 10 Then PriorityForDispatcher = DispatcherPriority.ContextIdle
                        ListeFichiers.Dispatcher.BeginInvoke(New UpdateListeRepInterfaceDelegate(AddressOf UpdateListeRepInterface),
                                                       PriorityForDispatcher,
                                                       {InfoFichierEnCours, FilesInfosUpdateOrder.update, PasRepEnCours, NumMiseAJour, ItemASelectionner})
                    Else
                        Exit Sub
                    End If
                    Compteur += 1
                Next
            End If
        Catch ex As Exception
        End Try
        ListeFichiers.Dispatcher.BeginInvoke(New UpdateListeRepInterfaceDelegate(AddressOf UpdateListeRepInterface),
                                             PriorityForDispatcher,
                                             {Nothing, FilesInfosUpdateOrder.fin, PasRepEnCours, NumMiseAJour, ""})
    End Sub
    'Voir fonction de TriListeFichiers

    Private Sub UpdateListeRepInterface(ByVal InfoFichier As tagID3FilesInfos, ByVal Ordre As FilesInfosUpdateOrder,
                                        ByVal PasRepEnCours As Boolean, ByVal NumMaJ As Long, ByVal ItemSelected As String)
        If NumMaJ = DemandeMaJ Then
            Select Case Ordre
                Case FilesInfosUpdateOrder.debut
                    MiseAJourEnCours = True
                    If PasRepEnCours Then
                        ShellWatcherMp3.IncludeSubdirectories = True
                        ShellWatcherWav.IncludeSubdirectories = True
                    Else
                        ShellWatcherMp3.IncludeSubdirectories = False
                        ShellWatcherWav.IncludeSubdirectories = False
                    End If
                    If RepertoireAMettreAJour <> "" Then
                        ShellWatcherMp3.Path = RepertoireAMettreAJour
                        ShellWatcherMp3.Filter = "*.mp3"
                        ShellWatcherMp3.NotifyFilter = NotifyFilters.LastWrite Or NotifyFilters.FileName
                        ShellWatcherMp3.EnableRaisingEvents = True
                        ShellWatcherWav.Path = RepertoireAMettreAJour
                        ShellWatcherWav.Filter = "*.wav"
                        ShellWatcherWav.NotifyFilter = NotifyFilters.LastWrite Or NotifyFilters.FileName
                        ShellWatcherWav.EnableRaisingEvents = True
                    End If
                    'If ListeFichiers.SelectedItem IsNot Nothing Then
                    Dim Bordure As Decorator = VisualTreeHelper.GetChild(ListeFichiers, 0)
                    Dim Scroll As ScrollViewer = Bordure.Child
                    Scroll.ScrollToTop()
                    'End If
                    RaiseEvent UpdateDebut()
                    _FilesCollection.Clear()
                Case (FilesInfosUpdateOrder.update)
                    If (RepertoireAMettreAJour = InfoFichier.Repertoire) Or (PasRepEnCours) Then
                        _FilesCollection.Add(New tagID3FilesInfosDO(InfoFichier))
                        If ListeFichiers.Items.Count = 1 Then
                            ListeFichiers.SelectedIndex = 0
                        End If
                    End If
                    If (ItemSelected <> "") And (SelectedItem <> ItemSelected) Then
                        SelectedItem = ItemSelected
                        If ListeFichiers.SelectedItem IsNot Nothing Then
                            ListeFichiers.ScrollIntoView(ListeFichiers.SelectedItem)
                        End If
                    End If
                    RaiseEvent UpdateEnCours()
                Case (FilesInfosUpdateOrder.fin)
                    MiseAJourEnCours = False
                    'If ItemSelected <> "" Then SelectedItem = ItemSelected
                    ' If ListeFichiers.SelectedItem IsNot Nothing Then
                    ' ListeFichiers.ScrollIntoView(ListeFichiers.SelectedItem)
                    ' End If
                    'ListeFichiers.Dispatcher.DisableProcessing()
                    RaiseEvent UpdateFin()
            End Select
        End If

    End Sub
    'REPOND AU MESSAGE DE MISE A JOUR DE LA SELECTION ET RENVOI LE MESSAGE AU PARENT
    Private Sub ListeFichiers_SelectionChanged(ByVal sender As Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles ListeFichiers.SelectionChanged
        For Each r As tagID3FilesInfosDO In e.RemovedItems
            r.Selected = False
        Next (r)
        For Each a As tagID3FilesInfosDO In e.AddedItems
            a.Selected = True
        Next (a)
        SelectedItems = New List(Of String)
        For Each i In ListeFichiers.SelectedItems
            SelectedItems.Add(CType(i, tagID3FilesInfosDO).NomComplet)
        Next
        RaiseEvent SelectionChanged(SelectedItems)
    End Sub
    Public Function GetNext() As String
        If (ListeFichiers.Items.Count > 0) And (ListeFichiers.SelectedIndex < 0) Then
            ListeFichiers.SelectedIndex = 0
            Return CType(ListeFichiers.SelectedItem, tagID3FilesInfosDO).NomComplet
            Return ListeFichiers.SelectedItem
        ElseIf (ListeFichiers.SelectedIndex >= 0) And (ListeFichiers.Items.Count > ListeFichiers.SelectedIndex) Then
            ListeFichiers.SelectedIndex += 1
            Return CType(ListeFichiers.SelectedItem, tagID3FilesInfosDO).NomComplet
        End If
        Return ""
    End Function
    '**************************************************************************************************************
    '*********************************GESTION DES ACTIONS SUR LA LISTE DES FICHIERS********************************
    '**************************************************************************************************************
    'PROCEDURES DE GESTION DU CLAVIER
    Private Sub ListeFichiers_PreviewKeyDown(ByVal sender As Object, ByVal e As System.Windows.Input.KeyEventArgs) Handles ListeFichiers.PreviewKeyDown
        Select Case e.Key
            Case Key.LeftShift
                If TexteLink IsNot Nothing Then
                    '  CType(TexteLink.ToolTip, ToolTip).Content = "Lancer une recherche sur le nom du fichier"
                    TexteLink.Foreground = Brushes.Red
                End If
            Case Key.Delete
                If TypeOf (e.OriginalSource) Is ListViewItem Then
                    Dim ListeFichiersAEffacer As New List(Of tagID3FilesInfosDO)
                    For Each i As tagID3FilesInfosDO In ListeFichiers.SelectedItems
                        ListeFichiersAEffacer.Add(i)
                    Next
                    ListeFichiersAEffacer.ForEach(Sub(Fichier As tagID3FilesInfosDO)
                                                      FileDelete(Fichier.NomComplet)
                                                  End Sub)
                End If
            Case Else
                'Debug.Print(e.Key.ToString)
        End Select
    End Sub
    Private Sub ListeFichiers_PreviewKeyUp(ByVal sender As Object, ByVal e As System.Windows.Input.KeyEventArgs) Handles ListeFichiers.PreviewKeyUp
        Select Case e.Key
            Case Key.LeftShift
                If TexteLink IsNot Nothing Then
                    '   CType(TexteLink.ToolTip, ToolTip).Content = "Lancer une recherche sur '" & ExtraitChaine(TexteLink.Name, "tagLink", "", 7) & "'"
                    TexteLink.Foreground = Brushes.Blue
                End If
            Case Else
                'Debug.Print(e.Key.ToString)
        End Select
    End Sub
    'FONCTION POUR EFFACER UN FICHIER
    Public Function FileDelete(ByVal FullName As String) As Boolean
        If ListeFichiers.SelectedItems.Count > 0 Then
            Try
                My.Computer.FileSystem.DeleteFile(FullName, Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, FileIO.RecycleOption.SendToRecycleBin, FileIO.UICancelOption.ThrowException)
                Return True
            Catch ex As Exception
            End Try
        End If
        Return False
    End Function
    'PROCEDURE DE GESTION DU MENU D'ENTETE DE LA LISTE
    Private Sub MenuContextuelHeaderListe_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles MenuContextuelHeaderListe.Loaded
        If TypeOf (ListeFichiers.View) Is GridView And Not DisableCustomisation Then
            MenuContextuelHeaderListe.Items.Clear()
            For Each i As GridViewColumn In CType(ListeFichiers.View, GridView).Columns
                If CType(i.Header, GridViewColumnHeader).Tag.ToString <> "Nom" Then
                    Dim UnItem As MenuItem = New MenuItem()
                    UnItem.Header = CType(i.Header, GridViewColumnHeader).Tag.ToString
                    UnItem.AddHandler(MenuItem.ClickEvent, New RoutedEventHandler(AddressOf ReponseMenuHeader))
                    If i.Width <> 0 Then UnItem.IsChecked = True
                    MenuContextuelHeaderListe.Items.Add(UnItem)
                End If
            Next
        End If
    End Sub
    Public Sub ReponseMenuHeader(ByVal Sender As Object, ByVal e As RoutedEventArgs)
        For Each i As GridViewColumn In CType(ListeFichiers.View, GridView).Columns
            If CType(i.Header, GridViewColumnHeader).Content.ToString = CType(Sender, MenuItem).Header Then
                If CType(Sender, MenuItem).IsChecked Then i.Width = 0 Else i.Width = Double.NaN
                Exit For
            End If
        Next
    End Sub

    'PROCEDURE DE REPONSE POUR LES DOUBLES CLICS SUR LA LISTE
    Private Sub ListeFichiers_PreviewMouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles ListeFichiers.MouseDoubleClick
        If TypeOf (e.OriginalSource) Is Border Then
            If CType(e.OriginalSource, Border).Name = "DragDropBorder" Then
                If ListeFichiers.SelectedItems.Count > 0 Then
                    RaiseEvent SelectionDblClick(CType(ListeFichiers.SelectedItem, tagID3FilesInfosDO).NomComplet, "")
                End If
            End If
        ElseIf TypeOf (e.OriginalSource) Is Image Then
            Dim Newurl As String = ""
            If CType(e.OriginalSource, Image).Name = "ImageFichier" Or CType(e.OriginalSource, Image).Name = "ImageFichierxml" Then
                Try
                    Newurl = CType(ListeFichiers.SelectedItem, tagID3FilesInfosDO).PageWeb
                    If Newurl <> "" Then
                        ' Newurl = "http://www.discogs.com/viewimages?release=" & ExtraitChaine(Texte, "/release", "", 9)
                        Dim NewURI As Uri = New Uri(Newurl)
                        RaiseEvent RequeteWebBrowser(NewURI)
                        e.Handled = True
                    End If
                Catch ex As Exception
                    MsgBox("Le lien """ & Newurl & """ est non valide", MsgBoxStyle.Exclamation Or MsgBoxStyle.OkOnly, "Lien URL non valide")
                End Try
            End If
        End If
    End Sub
    'PROCEDURE DE REPONSE POUR LES MENUS CONTEXTUELS DE LA LISTE
    Public Sub ReponseMenuListView(ByVal Sender As Object, ByVal e As RoutedEventArgs)
        For Each d As tagID3FilesInfosDO In ListeFichiers.SelectedItems
            Select Case CType(Sender, MenuItem).Name
                Case "IMSupprimerTagv1"
                    d.Id3v1Tag = False
                Case "IMSupprimerTagv2"
                    d.Id3v2Tag = False
                Case "IMNormaliserTagv2"
                    d.TagNormalise = True
            End Select
        Next
    End Sub

    'PROCEDURES D'APPEL RECHERCHE BIBLIOTHEQUE
    Private Sub Lien_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        Dim Lien As TextBlock = CType(e.OriginalSource, TextBlock)
        If Lien.Text <> "" Then
            Dim TexteRecherche As String = Lien.Text
            ' If (Keyboard.GetKeyStates(Key.LeftCtrl) And KeyStates.Down) > 0 Then
            ' 'REQUETE RECHERCHE DISCOGS
            ' RaiseEvent RequeteRecherche("fichier:" & Lien.Text, True)
            'Else
            Dim ShiftDown As Boolean = (Keyboard.GetKeyStates(Key.LeftShift) And KeyStates.Down) > 0
            Select Case Lien.Name
                Case "tagLinkArtiste"
                    RaiseEvent RequeteRecherche("artiste:" & TexteRecherche, Not ShiftDown)
                Case "tagLinkTitre"
                    RaiseEvent RequeteRecherche("titre:" & Trim(ExtraitChaine(TexteRecherche, "", "[", , True)), Not ShiftDown)
                Case "tagLinkLabel"
                    RaiseEvent RequeteRecherche("label:" & TexteRecherche, Not ShiftDown)
                Case "tagLinkCatalogue"
                    RaiseEvent RequeteRecherche("catalogue:" & TexteRecherche, Not ShiftDown)
                Case "tagLinkAnnee"
                    RaiseEvent RequeteRecherche("annee:" & TexteRecherche, Not ShiftDown)
                Case "tagLinkStyle"
                    RaiseEvent RequeteRecherche("style:" & TexteRecherche, Not ShiftDown)
                Case "tagLinkCompositeur"
                    RaiseEvent RequeteRecherche("compositeur:" & TexteRecherche, Not ShiftDown)
            End Select
            'End If
        End If
    End Sub

    '**************************************************************************************************************
    '**************************************GESTION DES MENUS CONTEXTUELS*******************************************
    '**************************************************************************************************************
    Private Async Sub ListeFichiers_PreviewMouseRightButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseRightButtonDown
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As tagID3FilesInfosDO = Nothing
        If TypeOf (ListeFichiers.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(ListeFichiers.ContainerFromElement(e.OriginalSource), ListViewItem)
            DonneeSurvolee = CType(ItemSurvole.Content, tagID3FilesInfosDO)
            CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).BorderBrush = Brushes.Transparent
        End If
        ' If (ItemSurvole IsNot Nothing) And (TypeOf (e.OriginalSource) Is Border) Then
        ' MiseAJourBloquee = True
        ' RaiseEvent RequeteSelectionRepertoire(DonneeSurvolee.Repertoire)
        ' MiseAJourBloquee = False
        ' End If
        If TypeOf (e.OriginalSource) Is Image Then
            If (CType(e.OriginalSource, Image).Name) = "ImageFichier" Or (CType(e.OriginalSource, Image).Name) = "ImageFichierxml" Then
                CType(e.OriginalSource, Image).ContextMenu = Await DonneeSurvolee.CreationMenuContextuelDynamique("Image")
            End If
        End If
        Dim ObjetEnCours As TextBox = CType(wpfApplication.FindAncestor(e.OriginalSource, "TextBox"), TextBox)
        If ObjetEnCours IsNot Nothing Then
            If ObjetEnCours.Name = "tagArtiste" Then ObjetEnCours.ContextMenu = Await DonneeSurvolee.CreationMenuContextuelDynamique("Artiste")
            If ObjetEnCours.Name = "tagTitre" Then ObjetEnCours.ContextMenu = Await DonneeSurvolee.CreationMenuContextuelDynamique("Titre")
            If ObjetEnCours.Name = "tagAlbum" Then ObjetEnCours.ContextMenu = Await DonneeSurvolee.CreationMenuContextuelDynamique("Album")
            If ObjetEnCours.Name = "tagCompositeur" Then ObjetEnCours.ContextMenu = Await DonneeSurvolee.CreationMenuContextuelDynamique("Compositeur")
            e.Handled = True
        End If
    End Sub
    Private Sub Informations_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        Dim Newurl As String = ""
        If ListeFichiers.SelectedItems.Count > 0 Then
            Try
                Newurl = CType(ListeFichiers.SelectedItem, tagID3FilesInfosDO).PageWeb
                If Newurl <> "" Then
                    Dim NewURI As Uri = New Uri(Newurl)
                    RaiseEvent RequeteWebBrowser(NewURI)
                    e.Handled = True
                Else
                    Dim TitreEnCours As String = CType(ListeFichiers.SelectedItem, tagID3FilesInfosDO).Titre
                    Dim ArtisteEnCours As String = CType(ListeFichiers.SelectedItem, tagID3FilesInfosDO).Artiste
                    If ArtisteEnCours <> "" Or TitreEnCours <> "" Then
                        Dim Texte As String = RemplaceOccurences(ArtisteEnCours & " " & ExtraitChaine(TitreEnCours, "", " [", , True), " ", "+")
                        Texte = RemplaceOccurences(Texte, "&", "")
                        Newurl = "http://www.discogs.com/search?q=" & Texte & "&type=all"
                        Dim NewURI As Uri = New Uri(Newurl)
                        RaiseEvent RequeteWebBrowser(NewURI)
                        e.Handled = True
                    End If

                End If
            Catch ex As Exception
                MsgBox("Le lien """ & Newurl & """ est non valide", MsgBoxStyle.Exclamation Or MsgBoxStyle.OkOnly, "Lien URL non valide")
            End Try
        End If
    End Sub
    Private Sub Extraction_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        If ListeFichiers.SelectedItems.Count > 0 Then
            For Each i In ListeFichiers.SelectedItems
                Me.Dispatcher.BeginInvoke(Sub(file As tagID3FilesInfosDO)
                                              file.Selected = True
                                              file.ExtractionInfosTitre()
                                              file.Selected = False
                                          End Sub, DispatcherPriority.Background, {CType(i, tagID3FilesInfosDO)})
            Next
        End If
    End Sub
    Private Sub SelectFolder_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        If ListeFichiers.SelectedItems.Count > 0 Then
            MiseAJourBloquee = True
            RaiseEvent RequeteSelectionRepertoire(CType(ListeFichiers.SelectedItem, tagID3FilesInfosDO).Repertoire)
            MiseAJourBloquee = False
        End If
    End Sub
    Private TempLockLink As Boolean
    Private Sub MenuContextuel_Opened(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        TempLockLink = True
    End Sub
    Private Sub MenuContextuel_Closed(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        TempLockLink = False
    End Sub

    '**************************************************************************************************************
    '**************************************GESTION DU TOOL DES IMAGES *********************************************
    '**************************************************************************************************************
    Private Sub Image_ToolTipOpening(ByVal sender As System.Object, ByVal e As System.Windows.Controls.ToolTipEventArgs)
        If TypeOf (e.OriginalSource) Is Image Then
            If CType(e.OriginalSource, Image).Name = "ImageFichierxml" Then
                Dim DockImage As DockPanel = CType(CType(e.OriginalSource, Image).ToolTip, DockPanel)
                Dim ItemSurvole = CType(ListeFichiers.ContainerFromElement(e.OriginalSource), ListViewItem)
                If ItemSurvole IsNot Nothing Then
                    Dim DonneeSurvolee As tagID3FilesInfosDO = CType(ItemSurvole.Content, tagID3FilesInfosDO)
                    Dim NomItem As tagID3FilesInfos = New tagID3FilesInfos(DonneeSurvolee.NomComplet)
                        If CType(DockImage.Tag, String) = NomItem.NomComplet Then Exit Sub
                        'If NomItem.idImage = "" Then
                        ' e.Handled = True
                        ' Exit Sub
                        'End If
                        DockImage.Tag = NomItem.NomComplet
                        Dim image As ImageSource = TagID3.tagID3Object.FonctionUtilite.DownLoadImage(NomItem.DataImage)
                        If image IsNot Nothing Then
                            Dim ImageDisque As Image = New Image()
                            ImageDisque.Height = 150
                            ImageDisque.Width = 150
                            ImageDisque.Stretch = Stretch.Fill
                            ImageDisque.Margin = New Thickness(2)
                            ImageDisque.Style = CType(ImageDisque.FindResource("GBImage"), Style)
                            ImageDisque.Source = image
                            DockImage.Children.Add(ImageDisque)
                        End If
                        Dim imageLabel As ImageSource = TagID3.tagID3Object.FonctionUtilite.DownLoadImage(NomItem.DataImageLabel)
                        If imageLabel IsNot Nothing Then
                            Dim ImageDisqueLabel As Image = New Image()
                            ImageDisqueLabel.Height = 150
                            ImageDisqueLabel.Width = 150
                            ImageDisqueLabel.Stretch = Stretch.Fill
                            ImageDisqueLabel.Margin = New Thickness(2)
                            ImageDisqueLabel.Style = CType(ImageDisqueLabel.FindResource("GBImage"), Style)
                            ImageDisqueLabel.Source = imageLabel
                            DockImage.Children.Add(ImageDisqueLabel)
                        End If
                        Dim imageDos As ImageSource = TagID3.tagID3Object.FonctionUtilite.DownLoadImage(NomItem.DataImageDosPochette)
                        If imageDos IsNot Nothing Then
                            Dim ImageDisqueDos As Image = New Image()
                            ImageDisqueDos.Height = 150
                            ImageDisqueDos.Width = 150
                            ImageDisqueDos.Stretch = Stretch.Fill
                            ImageDisqueDos.Margin = New Thickness(2)
                            ImageDisqueDos.Style = CType(ImageDisqueDos.FindResource("GBImage"), Style)
                            ImageDisqueDos.Source = imageDos
                            DockImage.Children.Add(ImageDisqueDos)
                        End If
                    End If
                    If DockImage.Children.Count = 0 Then
                        e.Handled = True
                        Exit Sub
                    End If
                End If
            End If
    End Sub

    '**************************************************************************************************************
    '**************************************GESTION DU DRAG AND DROP************************************************
    '**************************************************************************************************************
    Dim StartPoint As Point
    Dim TypeObjetClic As Object
    Dim PlateformVista As Boolean
    Private Sub ListeFichiers_PreviewMouseLeftButtonUp(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles ListeFichiers.PreviewMouseLeftButtonUp
        StartPoint = New Point
    End Sub

    Private Sub ListeFichiers_PreviewMouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles ListeFichiers.PreviewMouseLeftButtonDown
        StartPoint = e.GetPosition(ListeFichiers)
        TypeObjetClic = e.OriginalSource
        If (TypeOf (e.OriginalSource) Is ScrollViewer) Then ListeFichiers.SelectedItems.Clear()
        If TypeOf (e.OriginalSource) Is TextBlock Then
            If TempLockLink Then
                e.Handled = True
                Exit Sub
            End If
            If (CType(e.OriginalSource, TextBlock).Name Like "tagLink*") Then
                If (Keyboard.GetKeyStates(Key.LeftCtrl) And KeyStates.Down) = 0 Then
                    TexteLink = CType(e.OriginalSource, TextBlock)
                    Lien_Click(sender, e)
                    e.Handled = True
                Else
                    Dim Newurl As String = ""
                    Try
                        Select Case CType(e.OriginalSource, TextBlock).Name
                            Case "tagLinkid"
                                Newurl = "http://www.discogs.com/release/" & CType(e.OriginalSource, TextBlock).Text
                            Case "tagLinkArtiste"
                                Newurl = "http://www.discogs.com/search?q=" & CType(e.OriginalSource, TextBlock).Text & "&type=artists"
                            Case "tagLinkTitre"
                                Dim Chaine As String = Trim(ExtraitChaine(CType(e.OriginalSource, TextBlock).Text, "", "[", False))
                                If Chaine = "" Then Chaine = CType(e.OriginalSource, TextBlock).Text
                                Newurl = "http://www.discogs.com/search?q=" & Chaine & "&type=releases"
                            Case "tagLinkLabel"
                                Newurl = "http://www.discogs.com/search?q=" & CType(e.OriginalSource, TextBlock).Text & "&type=labels"
                            Case "tagLinkCatalogue"
                                Newurl = "http://www.discogs.com/search?q=" & CType(e.OriginalSource, TextBlock).Text & "&type=catalog#"
                            Case "tagLinkCompositeur"
                                Newurl = "http://www.discogs.com/search?q=" & CType(e.OriginalSource, TextBlock).Text & "&type=artists"
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
            If (CType(e.OriginalSource, TextBlock).Name Like "tagSort*") Then
                Debug.Print("SORT" & ExtraitChaine(CType(e.OriginalSource, TextBlock).Name, "tagSort", "", 7))
            End If
        End If
    End Sub
    Private Sub ListeFichiers_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles ListeFichiers.MouseLeave
        StartPoint = New Point
    End Sub
    Dim TexteLink As TextBlock = Nothing
    Dim ColonneSort As TextBlock = Nothing
    Private Sub ListeFichiers_PreviewMouseMove(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles ListeFichiers.PreviewMouseMove
        If ColonneSort IsNot Nothing Then
            ColonneSort.TextDecorations = Nothing
            ColonneSort.Cursor = Cursors.Arrow
            ColonneSort.Foreground = CType(ColonneSort.Tag, Brush) ' Brushes.Black
            ColonneSort = Nothing
            Debug.Print("clean")
        End If
        If TypeOf (e.OriginalSource) Is TextBlock Then
            If (CType(e.OriginalSource, TextBlock).Name Like "tagSort*") Then
                If ColonneSortEnCours IsNot Nothing Then
                    If ColonneSortEnCours.Name = CType(e.OriginalSource, TextBlock).Name Then Return
                End If
                ColonneSort = CType(e.OriginalSource, TextBlock)
                ColonneSort.Cursor = Cursors.Hand
                Dim myCollection As New TextDecorationCollection()
                myCollection.Add(New TextDecoration(TextDecorationLocation.Underline,
                                                                  Nothing, 0, TextDecorationUnit.Pixel,
                                                                  TextDecorationUnit.Pixel))
                ColonneSort.TextDecorations = myCollection
                ColonneSort.Tag = ColonneSort.Foreground
                ColonneSort.Foreground = New SolidColorBrush(Color.FromRgb(&H9F, &HBF, &HFF))
                Return
            End If
        End If
    End Sub
    Private Sub ListeFichiers_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles ListeFichiers.MouseMove
        If TexteLink IsNot Nothing Then
            TexteLink.TextDecorations = Nothing
            TexteLink.Cursor = Cursors.Arrow
            TexteLink.Foreground = CType(TexteLink.Tag, Brush) ' Brushes.Black
            TexteLink = Nothing
        End If
        If TypeOf (e.OriginalSource) Is Border And TypeOf (TypeObjetClic) Is Border Then
            If CType(e.OriginalSource, Border).Name = "DragDropBorder" Then
                Dim ItemComplet As Grid = CType(wpfApplication.FindAncestor(e.OriginalSource, "Grid"), Grid)
                Dim MousePos As Point = e.GetPosition(ListeFichiers)
                Dim Diff As Vector = StartPoint - MousePos
                If StartPoint.X <> 0 And StartPoint.Y <> 0 And (e.LeftButton = MouseButtonState.Pressed And Math.Abs(Diff.X) > 2 Or
                                                                Math.Abs(Diff.Y) > 2) Then
                    Dim FilesDataObject(ListeFichiers.SelectedItems.Count - 1) As String
                    Dim FilesTextName As String = ""
                    For i = 0 To ListeFichiers.SelectedItems.Count - 1
                        FilesDataObject(i) = CType(ListeFichiers.SelectedItems(i), tagID3FilesInfosDO).NomComplet
                        If FilesTextName = "" Then
                            FilesTextName = CType(ListeFichiers.SelectedItems(i), tagID3FilesInfosDO).Nom
                        Else
                            FilesTextName = FilesTextName & ";" & CType(ListeFichiers.SelectedItems(i), tagID3FilesInfosDO).Nom
                        End If
                    Next
                    If ListeFichiers.SelectedItems.Count > 0 Then
                        If PlateformVista Then
                            Dim TabParametres(3) As KeyValuePair(Of String, Object)
                            TabParametres(0) = New KeyValuePair(Of String, Object)(DataFormats.FileDrop, FilesDataObject)
                            TabParametres(1) = New KeyValuePair(Of String, Object)(DataFormats.Text, FilesTextName)
                            TabParametres(2) = New KeyValuePair(Of String, Object)("tagID3FilesInfosDO", ListeFichiers.SelectedItems(0))
                            Dim ListetagID3 As List(Of tagID3FilesInfosDO) = New List(Of tagID3FilesInfosDO)
                            For Each i In ListeFichiers.SelectedItems
                                ListetagID3.Add(i)
                            Next i
                            TabParametres(3) = New KeyValuePair(Of String, Object)("tagID3FilesInfosDO2", ListetagID3)
                            DragSourceHelper.DoDragDrop(ItemComplet, e.GetPosition(e.OriginalSource),
                                                    DragDropEffects.Move Or DragDropEffects.Copy, TabParametres)
                        Else
                            Dim data As DataObject = New DataObject()
                            data.SetData(DataFormats.FileDrop, FilesDataObject)
                            data.SetData(DataFormats.Text, FilesTextName)
                            data.SetData("tagID3FilesInfosDO", ListeFichiers.SelectedItems(0))
                            Dim effects As DragDropEffects = DragDrop.DoDragDrop(Me, data, DragDropEffects.Copy Or DragDropEffects.Move)
                        End If
                    End If
                End If
            End If
        Else
            If TypeOf (e.OriginalSource) Is Image And TypeOf (TypeObjetClic) Is Image Then
                Dim ItemSurvole As ListViewItem = CType(ListeFichiers.ContainerFromElement(e.OriginalSource), ListViewItem)
                Dim DonneeSurvolee As tagID3FilesInfosDO = CType(ItemSurvole.Content, tagID3FilesInfosDO)
                If (CType(e.OriginalSource, Image).Name) = "ImageFichier" Or (CType(e.OriginalSource, Image).Name) = "ImageFichierxml" Then
                    Dim MousePos As Point = e.GetPosition(ListeFichiers)
                    Dim Diff As Vector = StartPoint - MousePos
                    If StartPoint.X <> 0 And StartPoint.Y <> 0 And (e.LeftButton = MouseButtonState.Pressed And Math.Abs(Diff.X) > 2 Or
                                                                    Math.Abs(Diff.Y) > 2) Then
                        Dim FilesTextName As String = ""
                        For i = 0 To ListeFichiers.SelectedItems.Count - 1
                            If FilesTextName = "" Then
                                FilesTextName = CType(ListeFichiers.SelectedItems(i), tagID3FilesInfosDO).Nom
                            Else
                                FilesTextName = FilesTextName & ";" & CType(ListeFichiers.SelectedItems(i), tagID3FilesInfosDO).Nom
                            End If
                        Next
                        Dim BitmapData As MemoryStream
                        Using NewTagId3 As TagID3.tagID3Object = New TagID3.tagID3Object
                            NewTagId3.ForcageLecture = True
                            NewTagId3.FileNameMp3 = DonneeSurvolee.NomComplet
                            BitmapData = TagID3.tagID3Object.FonctionUtilite.ConvertJpegdataToDibstream(NewTagId3.ID3v2_ImageData)
                        End Using
                        If BitmapData IsNot Nothing Then
                            If PlateformVista Then
                                Dim TabParametres(0) As KeyValuePair(Of String, Object)
                                TabParametres(0) = New KeyValuePair(Of String, Object)(DataFormats.Dib, BitmapData)
                                DragSourceHelper.DoDragDrop(CType(e.OriginalSource, Image), e.GetPosition(e.OriginalSource),
                                                            DragDropEffects.Move Or DragDropEffects.Copy, TabParametres)
                            Else
                                Dim data As DataObject = New DataObject()
                                data.SetData(DataFormats.Dib, BitmapData)
                                Dim effects As DragDropEffects = DragDrop.DoDragDrop(Me, data, DragDropEffects.Copy Or DragDropEffects.Move)
                            End If
                        End If
                    End If
                End If
            ElseIf TypeOf (e.OriginalSource) Is TextBlock Then
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
    Dim ActionDropEnCours As String
    Private Sub ListeFichiers_Drop(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles ListeFichiers.Drop
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As tagID3FilesInfosDO = Nothing
        If TypeOf (ListeFichiers.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(ListeFichiers.ContainerFromElement(e.OriginalSource), ListViewItem)
            DonneeSurvolee = CType(ItemSurvole.Content, tagID3FilesInfosDO)
            CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).Opacity = 0
            ' CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).BorderBrush = Brushes.Transparent
        End If
        If (e.Data.GetDataPresent(DataFormats.Dib)) And (DonneeSurvolee IsNot Nothing) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            FileDrop(e, ItemSurvole)
        ElseIf (e.Data.GetDataPresent("fileCDAudio")) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            If (e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey Then
                FileCDAudioDrop(e, DonneeSurvolee, fileConverter.ConvertType.wav)
            Else
                FileCDAudioDrop(e, DonneeSurvolee, fileConverter.ConvertType.mp3)
            End If
        ElseIf (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
            If ((e.KeyStates And DragDropKeyStates.ShiftKey) = DragDropKeyStates.ShiftKey) And (DonneeSurvolee IsNot Nothing) And
                                            Not ForceCopyFiles Then
                e.Effects = e.AllowedEffects And DragDropEffects.Copy
                TagId3Drop(e, ItemSurvole)
            Else
                If ((e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey) Or ForceCopyFiles Then
                    e.Effects = e.AllowedEffects And DragDropEffects.Copy
                ElseIf (e.KeyStates And DragDropKeyStates.AltKey) = DragDropKeyStates.AltKey Then
                    e.Effects = e.AllowedEffects And DragDropEffects.Link
                Else
                    e.Effects = e.AllowedEffects And DragDropEffects.Move
                End If
                FileDrop(e, ItemSurvole)
            End If
        Else
            e.Effects = DragDropEffects.None
        End If
        If PlateformVista Then DropTargetHelper.Drop(e.Data, e.GetPosition(e.OriginalSource), e.Effects)
        e.Handled = True
    End Sub
    Private Sub ListeFichiers_DragEnter(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles ListeFichiers.DragEnter
        ' For Each c As String In e.Data.GetFormats()
        ' Debug.Print(c)
        ' Next
        ' Debug.Print("fin")
        Dim FlagPlateformVista As Boolean
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As tagID3FilesInfosDO = Nothing
        If TypeOf (ListeFichiers.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(ListeFichiers.ContainerFromElement(e.OriginalSource), ListViewItem)
            DonneeSurvolee = CType(ItemSurvole.Content, tagID3FilesInfosDO)
            CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).Opacity = 0.7
            ' CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).BorderBrush = CType(Me.FindResource("ItemBorderBrush"), SolidColorBrush)
        ElseIf wpfApplication.FindAncestor(e.OriginalSource, "ListView") IsNot Nothing Then
            ElementSurvolePrecedent = Nothing
            TempsDeDebutSurvole = Now.Ticks
            ExPosY = 0
        End If
        If (e.Data.GetDataPresent(DataFormats.Dib)) And (DonneeSurvolee IsNot Nothing) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                            e.Data, e.GetPosition(e.OriginalSource),
                                                            e.Effects, "Copier l'image vers la pochette de %1", DonneeSurvolee.Nom)
        ElseIf (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
            If ((e.KeyStates And DragDropKeyStates.ShiftKey) = DragDropKeyStates.ShiftKey) And
                                                                (DonneeSurvolee IsNot Nothing) And Not ForceCopyFiles Then
                e.Effects = e.AllowedEffects And DragDropEffects.Copy
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                e.Data, e.GetPosition(e.OriginalSource),
                                                                e.Effects, "Copier les TAGID3 vers %1", DonneeSurvolee.Nom)
            ElseIf RepertoireParDefaut <> "" Then
                If (((e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey) Or
                                                            (e.AllowedEffects And DragDropEffects.Move) = 0) Or
                                                            (ForceCopyFiles) Then
                    e.Effects = e.AllowedEffects And DragDropEffects.Copy
                    If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                    e.Data, e.GetPosition(e.OriginalSource),
                                                                    e.Effects, "Copier vers %1", RepertoireParDefaut)
                ElseIf (e.KeyStates And DragDropKeyStates.AltKey) = DragDropKeyStates.AltKey Then
                    e.Effects = e.AllowedEffects And DragDropEffects.Move
                    If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                    e.Data, e.GetPosition(e.OriginalSource),
                                                                    e.Effects, "Convertir en mp3 vers %1", RepertoireParDefaut)
                Else
                    e.Effects = e.AllowedEffects And DragDropEffects.Move
                    If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                    e.Data, e.GetPosition(e.OriginalSource),
                                                                    e.Effects, "Déplace vers %1", RepertoireParDefaut)
                End If
            Else
                e.Effects = DragDropEffects.None
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                e.Data, e.GetPosition(e.OriginalSource),
                                                                e.Effects, "Copie impossible sur ce type de sélection", "")
            End If
            'e.Handled = True
        Else
            e.Effects = DragDropEffects.None
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                            e.Data, e.GetPosition(e.OriginalSource),
                                                            e.Effects, "", "")
            e.Handled = True
        End If
    End Sub
    Private Sub ListeFichiers_DragLeave(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles ListeFichiers.DragLeave
        Dim ItemSurvole As ListViewItem
        If TypeOf (ListeFichiers.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(ListeFichiers.ContainerFromElement(e.OriginalSource), ListViewItem)
            CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).Opacity = 0
            '   CType(ItemSurvole.Template.FindName("DragDropBorder", ItemSurvole), Border).BorderBrush = Brushes.Transparent
        ElseIf TypeOf (ListeFichiers.ContainerFromElement(e.OriginalSource)) Is ListView Then
            ElementSurvolePrecedent = Nothing
            ExPosY = 0
        End If
        If PlateformVista Then
            DropTargetHelper.DragLeave(e.Data)
        End If
        e.Handled = True
    End Sub
    Dim ElementSurvolePrecedent As tagID3FilesInfosDO
    Dim TempsDeDebutSurvole As Long
    Dim ExPosY As Double
    Private Sub ListeFichiers_DragOver(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles ListeFichiers.DragOver
        Static compteur As Integer
        Dim FlagPlateformVista As Boolean
        Dim Tempo As TimeSpan = New TimeSpan(Now.Ticks - TempsDeDebutSurvole)
        If Tempo.TotalSeconds > 0.5 Then
            If (e.GetPosition(ListeFichiers).Y <= ExPosY) And (e.GetPosition(ListeFichiers).Y < 40) Then
                CType(ListeFichiers.Template.FindName("GBScrollViewer", ListeFichiers), ScrollViewer).LineUp()
            End If
            If ((ExPosY <> 0) And (e.GetPosition(ListeFichiers).Y >= ExPosY) And (e.GetPosition(ListeFichiers).Y > (ListeFichiers.ActualHeight - 40))) Then
                CType(ListeFichiers.Template.FindName("GBScrollViewer", ListeFichiers), ScrollViewer).LineDown()
                Debug.Print(e.GetPosition(ListeFichiers).Y & " - " & ExPosY)
            End If
        End If
        ExPosY = e.GetPosition(ListeFichiers).Y
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        Dim ItemSurvole As ListViewItem = Nothing
        Dim DonneeSurvolee As tagID3FilesInfosDO = Nothing
        If TypeOf (ListeFichiers.ContainerFromElement(e.OriginalSource)) Is ListViewItem Then
            ItemSurvole = CType(ListeFichiers.ContainerFromElement(e.OriginalSource), ListViewItem)
            DonneeSurvolee = CType(ItemSurvole.Content, tagID3FilesInfosDO)
            'GESTION DU TEMPS DE SURVOLE DE L'ELEMENT POUR CHANGEMENT SELECTION
            If DonneeSurvolee.Equals(ElementSurvolePrecedent) Then
                Dim Intervale As TimeSpan = New TimeSpan(Now.Ticks - TempsDeDebutSurvole)
                If Intervale.TotalSeconds > 2 Then
                    Debug.Print(Intervale.ToString)
                    ListeFichiers.SelectedItem = DonneeSurvolee
                End If
            Else
                ElementSurvolePrecedent = DonneeSurvolee
                TempsDeDebutSurvole = Now.Ticks
            End If
        End If
        If (e.Data.GetDataPresent(DataFormats.Dib)) And (DonneeSurvolee IsNot Nothing) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragOver(e.GetPosition(e.OriginalSource), e.Effects)
        ElseIf (e.Data.GetDataPresent("fileCDAudio")) Then
            If RepertoireParDefaut <> "" Then
                e.Effects = e.AllowedEffects And DragDropEffects.Copy
                If (e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey Then
                    If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                            e.Data, e.GetPosition(e.OriginalSource),
                                                                            e.Effects, "Copier vers %1", RepertoireParDefaut)
                Else
                    If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                  e.Data, e.GetPosition(e.OriginalSource),
                                                                  e.Effects, "Convertir en mp3 vers %1", RepertoireParDefaut)
                End If
            Else
                e.Effects = DragDropEffects.None
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                               e.Data, e.GetPosition(e.OriginalSource),
                                                                e.Effects, "Copie ou conversion impossible sur cette sélection", "")
            End If
        ElseIf (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
            If ((e.KeyStates And DragDropKeyStates.ShiftKey) = DragDropKeyStates.ShiftKey) And (DonneeSurvolee IsNot Nothing) And
                                                                                                Not ForceCopyFiles Then
                e.Effects = e.AllowedEffects And DragDropEffects.Copy
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                e.Data, e.GetPosition(e.OriginalSource),
                                                                e.Effects, "Copier les TAGID3 vers %1", DonneeSurvolee.Nom)
                'ActionCopierTagEnCours = True
                ActionDropEnCours = "DropTagEnCours"
            ElseIf RepertoireParDefaut <> "" Then
                If (((e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey) Or
                                                                        ((e.AllowedEffects And DragDropEffects.Move) = 0)) Or
                                                                                ForceCopyFiles Then
                    e.Effects = e.AllowedEffects And DragDropEffects.Copy
                    If ActionDropEnCours <> "DropCopyEnCours" Then
                        ' If Not ActionCopierEnCours Or ActionCopierTagEnCours Then
                        compteur = compteur + 1
                        Debug.Print("passage " & compteur.ToString)
                        If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource), e.Data,
                                                          e.GetPosition(e.OriginalSource), e.Effects,
                                                          "Copier vers %1", RepertoireParDefaut)
                        'ActionCopierEnCours = True
                        'ActionCopierTagEnCours = False
                        ActionDropEnCours = "DropCopyEnCours"
                    Else
                        If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragOver(e.GetPosition(e.OriginalSource), e.Effects)
                    End If
                ElseIf ((e.KeyStates And DragDropKeyStates.AltKey) = DragDropKeyStates.AltKey) Then
                    If ActionDropEnCours <> "DropMp3EnCours" Then
                        e.Effects = e.AllowedEffects And DragDropEffects.Move
                        If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                      e.Data, e.GetPosition(e.OriginalSource),
                                                                      e.Effects, "Convertir en mp3 vers %1", RepertoireParDefaut)
                        'ActionCopierEnCours = False
                        'ActionCopierTagEnCours = False
                        ActionDropEnCours = "DropMp3EnCours"
                    Else
                        If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragOver(e.GetPosition(e.OriginalSource), e.Effects)
                    End If
                Else
                    e.Effects = e.AllowedEffects And DragDropEffects.Move
                    'If ActionCopierEnCours Or ActionCopierTagEnCours Then
                    If ActionDropEnCours <> "DropMoveEnCours" Then
                        If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource), e.Data,
                                                                                e.GetPosition(e.OriginalSource), e.Effects,
                                                                                "Déplace vers %1", RepertoireParDefaut) ' CType(Me.Template.FindName("GBTextBlock", Me), textblock).he)
                        'ActionCopierEnCours = False
                        'ActionCopierTagEnCours = False
                        ActionDropEnCours = "DropMoveEnCours"
                    Else
                        If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragOver(e.GetPosition(e.OriginalSource), e.Effects)
                    End If
                End If
            Else
                e.Effects = DragDropEffects.None
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                    e.Data, e.GetPosition(e.OriginalSource),
                                                                    e.Effects, "Copie impossible sur ce type de sélection", "")
            End If
        Else
            e.Effects = DragDropEffects.None
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                e.Data, e.GetPosition(e.OriginalSource),
                                                                e.Effects, "", "")
        End If
        'Me.BringIntoView()
        e.Handled = True
    End Sub

    'FONCTION DE TRAITEMENT DU DROP PISTE CD
    Private Sub FileCDAudioDrop(ByVal e As System.Windows.DragEventArgs, ByVal Item As tagID3FilesInfosDO,
                                Optional ByVal typeConversion As fileConverter.ConvertType = fileConverter.ConvertType.mp3)
        Dim Chemin As String
        If e.Data.GetDataPresent("FileCDAudio") Then
            If Item IsNot Nothing Then
                Chemin = Path.GetDirectoryName(Item.NomComplet)
            ElseIf RepertoireParDefaut <> "" Then
                Chemin = RepertoireParDefaut
            Else
                Exit Sub
            End If
            Dim Info As Object = e.Data.GetData("FileCDAudio")
            Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           RaiseEvent RequeteConvertFile(CType(Info, String()),
                                                                                         Chemin,
                                                                                         typeConversion)
                                                       End Sub))
        End If
    End Sub
    'FONCTION DE TRAITEMENT DU DROP SUR LA LISTE
    Private Sub FileDrop(ByVal e As System.Windows.DragEventArgs, Optional ByVal Item As ListViewItem = Nothing)
        Dim Formats As String() = e.Data.GetFormats()
        Dim Chemin As String = ""
        Dim NomDirectory As String = ""
        Dim NomFichier As String = ""
        If e.Data.GetDataPresent(DataFormats.Dib) Then
            If Item IsNot Nothing Then
                Dim DonneeSurvolee As tagID3FilesInfosDO = CType(Item.Content, tagID3FilesInfosDO)
                Using NewTagId3 As TagID3.tagID3Object = New TagID3.tagID3Object(DonneeSurvolee.NomComplet, True)
                    NewTagId3.ID3v2_SetImage(TagID3.tagID3Object.FonctionUtilite.ConvertDibstreamToJpegdata(
                                                CType(e.Data.GetData(DataFormats.Dib), System.IO.MemoryStream)))
                End Using
            End If
        End If
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            Dim Info As Object = e.Data.GetData(DataFormats.FileDrop)
            For Each j In CType(Info, String())
                Try
                    If Item IsNot Nothing Then
                        Chemin = Path.GetDirectoryName(CType(Item.Content, tagID3FilesInfosDO).NomComplet)
                    ElseIf RepertoireParDefaut <> "" Then
                        Chemin = RepertoireParDefaut
                    Else
                        Exit For
                    End If
                    If FileIO.FileSystem.DirectoryExists(j) Then 'CAS D'UN REPERTOIRE
                        NomDirectory = Path.GetFileName(j)
                        If ((e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey) Or ForceCopyFiles Then 'COPY DIRECTORY
                            Do Until Not My.Computer.FileSystem.DirectoryExists(Chemin & "\" & NomDirectory)
                                NomDirectory = "Copie de " & NomDirectory
                            Loop
                            My.Computer.FileSystem.CopyDirectory(j, Chemin & "\" & NomDirectory, FileIO.UIOption.AllDialogs, FileIO.UICancelOption.ThrowException)
                            If ExecuteDirectoryDropAction IsNot Nothing Then
                                Dim FunctionAExecuter As DelegateSpecificDropAction = ExecuteDirectoryDropAction
                                FunctionAExecuter(Chemin & "\" & NomDirectory)
                            End If
                        Else 'MOVE DIRECTORY
                            My.Computer.FileSystem.MoveDirectory(j, Chemin & "\" & NomDirectory, FileIO.UIOption.AllDialogs, FileIO.UICancelOption.ThrowException)
                        End If
                    Else 'If FileIO.FileSystem.FileExists(j) Then 'CAS D'UN FICHIER
                        NomFichier = Path.GetFileName(j)
                        If ((e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey) Or
                                                                                                ForceCopyFiles Then 'COPY FILE
                            Do Until Not My.Computer.FileSystem.FileExists(Chemin & "\" & NomFichier)
                                NomFichier = "Copie de " & NomFichier
                            Loop
                            My.Computer.FileSystem.CopyFile(j, Chemin & "\" & NomFichier, FileIO.UIOption.AllDialogs, FileIO.UICancelOption.ThrowException)
                            If ExecuteFileDropAction IsNot Nothing Then
                                Dim FunctionAExecuter As DelegateSpecificDropAction = ExecuteFileDropAction
                                Me.Dispatcher.BeginInvoke(Sub(FichierATraiter As String)
                                                              FunctionAExecuter(FichierATraiter)
                                                          End Sub, DispatcherPriority.Background, {Chemin & "\" & NomFichier})
                            End If
                        ElseIf (e.KeyStates And DragDropKeyStates.AltKey) = DragDropKeyStates.AltKey Then 'CONVERTIR
                            Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                                     New NoArgDelegate(Sub()
                                                                           RaiseEvent RequeteConvertFile({j},
                                                                                                         Chemin,
                                                                                                         fileConverter.ConvertType.mp3)
                                                                       End Sub))

                        Else 'MOVE FILE
                            My.Computer.FileSystem.MoveFile(j, Chemin & "\" & NomFichier, FileIO.UIOption.AllDialogs, FileIO.UICancelOption.ThrowException)
                        End If
                    End If
                Catch ex As Exception
                End Try
            Next
        End If
    End Sub
    'FONCTION DE TRAITEMENT DU DROP TAG SUR LA LISTE
    Private Sub TagId3Drop(ByVal e As System.Windows.DragEventArgs, ByVal Item As ListViewItem)
        If (Item IsNot Nothing) And e.Data.GetDataPresent(DataFormats.FileDrop) Then
            Dim Origine As String = CType(e.Data.GetData(DataFormats.FileDrop), String())(0)
            Dim Destination As String = CType(Item.Content, tagID3FilesInfosDO).NomComplet
            Using NewTagId3 As TagID3.tagID3Object = New TagID3.tagID3Object(Destination, True)
                Dim TagId3Origine As TagID3.tagID3Object = New TagID3.tagID3Object(Origine)
                Dim NomArtiste As String = NewTagId3.ID3v2_Texte("TPE1")
                Dim NomTitre As String = NewTagId3.ID3v2_Texte("TIT2")
                If TagId3Origine.FileNameMp3 IsNot Nothing Then NewTagId3.ID3v2_Frames = TagId3Origine.ID3v2_Frames
                If NomArtiste <> "" Then NewTagId3.ID3v2_Texte("TPE1") = NomArtiste
                If NomTitre <> "" Then NewTagId3.ID3v2_Texte("TIT2") = NomTitre
            End Using
        End If
    End Sub

    '**************************************************************************************************************
    '*****************************GESTION DE LA MISE A JOUR AUTOMATIQUE DE LA LISTE********************************
    '**************************************************************************************************************
    'PROCEDURE DE MISE A JOUR DE LA LISTE DES FICHIERS
    'Traitement du message lors de la notification de creation d'un fichier
    Dim ExFichierSupprime As tagID3FilesInfosDO = Nothing
    Private Sub ShellWatcher_Created(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles ShellWatcherMp3.Created,
                                                                                                                ShellWatcherWav.Created
        If BibliothequeLiee Is Nothing Then
            NotifyShellWatcherFilesCreated(e, Nothing)
        Else
            If (Not BibliothequeLiee.MiseAJourOk) Or ((Not RequeteXElement) And (Path.GetExtension(e.FullPath) = ".wav")) Then
                NotifyShellWatcherFilesCreated(e, Nothing)
            End If
        End If
    End Sub
    'Traitement du message lors de la notification d'effacement d'un fichier
    Private Sub ShellWatcher_Deleted(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles ShellWatcherMp3.Deleted,
                                                                                                                ShellWatcherWav.Deleted
        If BibliothequeLiee Is Nothing Then
            NotifyShellWatcherFilesDeleted(e)
        Else
            If (Not BibliothequeLiee.MiseAJourOk) Or ((Not RequeteXElement) And (Path.GetExtension(e.FullPath) = ".wav")) Then
                NotifyShellWatcherFilesDeleted(e)
            End If
        End If
    End Sub
    'Traitement du message lors de la notification de modification d'un fichier
    Private Sub ShellWatcher_Changed(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles ShellWatcherMp3.Changed,
                                                                                                                ShellWatcherWav.Changed
        If BibliothequeLiee Is Nothing Then
            NotifyShellWatcherFilesChanged(e, Nothing)
        Else
            If (Not BibliothequeLiee.MiseAJourOk) Or ((Not RequeteXElement) And (Path.GetExtension(e.FullPath) = ".wav")) Then
                NotifyShellWatcherFilesChanged(e, Nothing)
            End If
        End If
    End Sub
    'Traitement du message lors de la notification de renommage d'un fichier
    Private Sub ShellWatcher_Renamed(ByVal sender As Object, ByVal e As System.IO.RenamedEventArgs) Handles ShellWatcherMp3.Renamed,
                                                                                                            ShellWatcherWav.Renamed
        If BibliothequeLiee Is Nothing Then
            NotifyShellWatcherFilesRenamed(e, Nothing)
        Else
            If (Not BibliothequeLiee.MiseAJourOk) Or ((Not RequeteXElement) And (Path.GetExtension(e.FullPath) = ".wav")) Then
                NotifyShellWatcherFilesRenamed(e, Nothing)
            End If
        End If
    End Sub

    Public Sub NotifyShellWatcherFilesCreated(ByVal e As System.IO.FileSystemEventArgs, ByVal Infos As tagID3FilesInfos) Implements iNotifyShellUpdate.NotifyShellWatcherFilesCreated
        Dim Priorite As DispatcherPriority = DispatcherPriority.Input
        If MiseAJourEnCours Then
            Priorite = DispatcherPriority.SystemIdle
            Thread.Sleep(500)
        End If
        ListeFichiers.Dispatcher.BeginInvoke(Priorite,
                                 New NoArgDelegate(Sub()
                                                       Dim VerifierTaille As Boolean = False
                                                       If RepertoireParDefaut = "" Then
                                                           If ExFichierSupprime IsNot Nothing Then
                                                               If (ExFichierSupprime.Nom = Path.GetFileNameWithoutExtension(e.Name)) Then VerifierTaille = True
                                                           End If
                                                           If (Not VerifierTaille) Then Exit Sub
                                                       End If
                                                       If Infos Is Nothing Then Infos = New tagID3FilesInfos(e.FullPath)
                                                       Dim NewFichier As tagID3FilesInfosDO = New tagID3FilesInfosDO(Infos)
                                                       If VerifierTaille Then If (NewFichier.Taille = ExFichierSupprime.Taille) Then VerifierTaille = False
                                                       If Not VerifierTaille Then
                                                           Dim MiseAJourAFaire As Boolean = True
                                                           For Each i In _FilesCollection
                                                               If i.NomComplet = NewFichier.NomComplet Then
                                                                   MiseAJourAFaire = False
                                                                   Exit For
                                                               End If
                                                           Next
                                                            If MiseAJourAFaire Then
                                                               _FilesCollection.Add(NewFichier)
                                                               RaiseEvent UpdateEnCours()
                                                           End If
                                                           If ListeFichiers.Items.Count = 1 Then
                                                               ListeFichiers.SelectedIndex = 0
                                                           End If
                                                       End If
                                                   End Sub))
    End Sub
    Public Sub NotifyShellWatcherFilesDeleted(ByVal e As System.IO.FileSystemEventArgs) Implements iNotifyShellUpdate.NotifyShellWatcherFilesDeleted
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                 New NoArgDelegate(Sub()
                                                       For i As Integer = _FilesCollection.Count - 1 To 0 Step -1
                                                           If _FilesCollection.Item(i)._PathOriginal = e.FullPath Then
                                                               If RepertoireParDefaut = "" Then ExFichierSupprime = _FilesCollection.Item(i)
                                                               _FilesCollection.RemoveAt(i)
                                                               RaiseEvent UpdateEnCours()
                                                               Exit For
                                                           End If
                                                       Next
                                                   End Sub))
    End Sub
    Public Sub NotifyShellWatcherFilesChanged(ByVal e As System.IO.FileSystemEventArgs, ByVal Infos As tagID3FilesInfos) Implements iNotifyShellUpdate.NotifyShellWatcherFilesChanged
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                 New NoArgDelegate(Sub()
                                                       If Infos Is Nothing Then Infos = New tagID3FilesInfos(e.FullPath)
                                                       If Infos.NomComplet IsNot Nothing Then
                                                           For Each i In _FilesCollection
                                                               If i.NomComplet = e.FullPath Then
                                                                   i.Update(Infos)
                                                                   Exit For
                                                               End If
                                                           Next
                                                       End If
                                                   End Sub))
    End Sub
    Public Sub NotifyShellWatcherFilesRenamed(ByVal e As System.IO.RenamedEventArgs, ByVal Infos As tagID3FilesInfos) Implements iNotifyShellUpdate.NotifyShellWatcherFilesRenamed
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                 New NoArgDelegate(Sub()
                                                       If Path.GetExtension(e.FullPath) = ".mp3" Or Path.GetExtension(e.FullPath) = ".wav" Then
                                                           If Path.GetExtension(e.OldFullPath) = ".~tw" Or Path.GetExtension(e.OldFullPath) = ".~t3" Then
                                                               NotifyShellWatcherFilesCreated(e, Infos)
                                                           ElseIf (Path.GetExtension(e.OldFullPath) = ".~p3" And (Path.GetFileNameWithoutExtension(e.OldFullPath) = Path.GetFileNameWithoutExtension(e.FullPath))) Then
                                                               NotifyShellWatcherFilesChanged(e, Infos)
                                                           Else
                                                               Dim MiseAJourOk As Boolean = False
                                                               For Each i In _FilesCollection
                                                                   If i.NomComplet = e.OldFullPath Or i.NomComplet = e.FullPath Then
                                                                       If Infos Is Nothing Then Infos = New tagID3FilesInfos(e.FullPath)
                                                                       i.Update(Infos)
                                                                       MiseAJourOk = True
                                                                       Exit For
                                                                   End If
                                                               Next
                                                               For i = 0 To SelectedItems.Count - 1
                                                                   If SelectedItems.Item(i) = e.OldFullPath Then SelectedItems.Item(i) = e.FullPath
                                                               Next
                                                               If Not MiseAJourOk Then
                                                                   _FilesCollection.Add(New tagID3FilesInfosDO(Infos))
                                                               End If
                                                           End If
                                                       End If
                                                   End Sub))
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DU TRI DANS LA LISTE DES FICHIERS---------------------
    '***********************************************************************************************
    Private CollectionView As ICollectionView
    Private ColonneTriEnCours As GridViewColumnHeader
    Private IconeDeTriEnCours As AdornerSort
    Private DirectionEnCours As ListSortDirection
    Private ColonneTemporaireTri As GridViewColumnHeader
    Private IconeDeTriTemporaire As AdornerSort
    Private NomColonneTriEnCours As String
    Private NomChampTriEnCours As String
    Private SensColonneTriEnCours As ListSortDirection
    Private ColonneSortEnCours As TextBlock = Nothing

    'Procedure de réponse à une demande de tri sur une des colonne de la liste
    Public Sub ActiveTri(ByVal ChampATrier As String, Optional ByVal NouvelleDirection As ListSortDirection = ListSortDirection.Ascending)
        Dim Colonne As GridViewColumnHeader = Nothing
        NomChampTriEnCours = ExtraitChaine(ChampATrier, "[", "]")
        ChampATrier = ExtraitChaine(ChampATrier, "", "[")
        If CollectionView Is Nothing Then CollectionView = CollectionViewSource.GetDefaultView(FilesCollection)
        For Each i As GridViewColumn In CType(ListeFichiers.View, GridView).Columns
            If CType(i.Header, GridViewColumnHeader).Tag = ChampATrier Then
                Colonne = CType(i.Header, GridViewColumnHeader)
                If NomChampTriEnCours <> "" Then
                    For Each j In CType(Colonne.Content, DockPanel).Children
                        If TypeOf (j) Is TextBlock Then
                            If (CType(j, TextBlock).Name Like "*" & NomChampTriEnCours) Then
                                ColonneSortEnCours = j
                                ColonneSortEnCours.Tag = ColonneSortEnCours.Foreground
                                ColonneSortEnCours.Foreground = New SolidColorBrush(Color.FromRgb(&H9F, &HBF, &HFF))
                                Exit For
                            End If
                        End If
                    Next
                End If
                Exit For
            End If
        Next
        If Colonne Is Nothing Then Exit Sub
        If ColonneTriEnCours IsNot Nothing Then
            AdornerLayer.GetAdornerLayer(Colonne).Remove(IconeDeTriEnCours)
            CollectionView.SortDescriptions.Clear()
        End If
        If ColonneTriEnCours IsNot Nothing Then
            If (ColonneTriEnCours.Tag = Colonne.Tag) And (IconeDeTriEnCours.Direction = NouvelleDirection) Then
                NouvelleDirection = ListSortDirection.Descending
            End If
        End If
        ColonneTriEnCours = Colonne
        IconeDeTriEnCours = New AdornerSort(ColonneTriEnCours, NouvelleDirection)
        AdornerLayer.GetAdornerLayer(ColonneTriEnCours).Add(IconeDeTriEnCours)
        NomColonneTriEnCours = ChampATrier ' ColonneTriEnCours.Tag
        Dim ChampEncours As String = NomColonneTriEnCours
        If NomChampTriEnCours <> "" Then ChampEncours = NomChampTriEnCours
        CollectionView.SortDescriptions.Add(New SortDescription(ChampEncours, NouvelleDirection))
        CollectionView.SortDescriptions.Add(New SortDescription("Nom", ListSortDirection.Ascending))
        SensColonneTriEnCours = NouvelleDirection
    End Sub
    'Procedure de réponse à une demande de tri sur une des colonne de la liste
    Public Sub SortClick(ByVal Sender As Object, ByVal e As RoutedEventArgs)
        If BackUpRechercheEnCours = "" Then Exit Sub
        Dim Colonne As GridViewColumnHeader = CType(Sender, GridViewColumnHeader)
        Dim ChampATrier As String = Colonne.Tag
        Dim SousChampATrier As String = ""
        If ColonneSort IsNot Nothing Then
            SousChampATrier = ExtraitChaine(ColonneSort.Name, "tagSort", "", 7)
            If SousChampATrier <> NomChampTriEnCours Then
                If ColonneSortEnCours IsNot Nothing Then
                    ColonneSortEnCours.Tag = ColonneSort.Tag
                    ColonneSortEnCours.Foreground = CType(ColonneSort.Tag, Brush) ' Brushes.Black
                    ColonneSortEnCours.TextDecorations = Nothing
                End If
                ColonneSortEnCours = ColonneSort
            End If
            ColonneSort.TextDecorations = Nothing
            ColonneSort = Nothing
        Else
            If ChampATrier <> "Artiste" Then
                If ColonneSortEnCours IsNot Nothing Then
                    ColonneSortEnCours.Foreground = CType(ColonneSortEnCours.Tag, Brush) ' Brushes.Black
                    ColonneSortEnCours = Nothing
                End If
            Else
                If ColonneSortEnCours IsNot Nothing Then
                    SousChampATrier = ExtraitChaine(ColonneSortEnCours.Name, "tagSort", "", 7)
                End If
            End If
        End If
        If (ChampATrier <> "Artiste") Or (SousChampATrier <> "") Then
            If ColonneTemporaireTri Is Nothing Then
                If ColonneTriEnCours IsNot Nothing Then
                    AdornerLayer.GetAdornerLayer(Colonne).Remove(IconeDeTriEnCours)
                    CollectionView.SortDescriptions.Clear()
                End If
                Dim NouvelleDirection As ListSortDirection = ListSortDirection.Ascending
                If ColonneTriEnCours IsNot Nothing Then
                    If (ColonneTriEnCours.Tag = Colonne.Tag) And (IconeDeTriEnCours.Direction = NouvelleDirection) Then
                        If (ChampATrier = NomColonneTriEnCours) And (SousChampATrier = NomChampTriEnCours) Then
                            NouvelleDirection = ListSortDirection.Descending
                        End If
                    End If
                End If
                ColonneTriEnCours = Colonne
                IconeDeTriEnCours = New AdornerSort(ColonneTriEnCours, NouvelleDirection)
                AdornerLayer.GetAdornerLayer(ColonneTriEnCours).Add(IconeDeTriEnCours)
                NomColonneTriEnCours = ChampATrier ' ColonneTriEnCours.Tag
                NomChampTriEnCours = SousChampATrier
                SensColonneTriEnCours = NouvelleDirection
                If BackUpRechercheEnCours <> "" Then
                    _FilesCollection.Clear()
                    Dim ChampEncours As String = NomColonneTriEnCours
                    If SousChampATrier <> "" Then ChampEncours = SousChampATrier
                    CollectionView.SortDescriptions.Add(New SortDescription(ChampEncours, NouvelleDirection))
                    CollectionView.SortDescriptions.Add(New SortDescription("Nom", ListSortDirection.Ascending))
                    If RequeteXElement Then
                        MiseAJourListeFichierXElement(BackUpRechercheEnCours, RepertoireParDefaut)
                    Else
                        MiseAJourListeRepertoire(RepertoireParDefaut)
                    End If
                    'Else
                    '    CollectionView.SortDescriptions.Add(New SortDescription(ChampATrier, NouvelleDirection))
                    '    CollectionView.SortDescriptions.Add(New SortDescription("Nom", ListSortDirection.Ascending))
                End If
                End If
            End If
    End Sub
    'Procedure de rafraichissement du tri
    Public Sub RefreshSort()
        If ColonneTriEnCours IsNot Nothing Then
            ListeFichiers.Items.SortDescriptions.Clear()
            If BackUpRechercheEnCours <> "" Then
                MiseAJourListeFichierXElement(BackUpRechercheEnCours, RepertoireParDefaut)
            Else
                ListeFichiers.Items.SortDescriptions.Add(New SortDescription(ColonneTriEnCours.Tag, IconeDeTriEnCours.Direction))
                ListeFichiers.Items.SortDescriptions.Add(New SortDescription("Nom", ListSortDirection.Ascending))
            End If
        End If
    End Sub

    Public Function TriListeFichiers(ByVal Liste As IEnumerable(Of XElement)) As IEnumerable(Of XElement)
        Dim ChaineDeTri As String = NomColonneTriEnCours
        If NomChampTriEnCours <> "" Then ChaineDeTri = NomChampTriEnCours
        Select Case ChaineDeTri
            Case "Nom"
                If SensColonneTriEnCours = ListSortDirection.Ascending Then
                    Return From i In Liste
                                 Order By i.<NomFichier>.Value Ascending
                Else
                    Return From i In Liste
                                Order By i.<NomFichier>.Value Descending
                End If
            Case "Artiste"
                If SensColonneTriEnCours = ListSortDirection.Ascending Then
                    Return From i In Liste
                                 Order By i.<Artiste>.Value Ascending, i.<NomFichier>.Value Ascending
                Else
                    Return From i In Liste
                                Order By i.<Artiste>.Value Descending, i.<NomFichier>.Value Ascending
                End If
            Case "Titre"
                If SensColonneTriEnCours = ListSortDirection.Ascending Then
                    Return From i In Liste
                                 Order By i.<Titre>.Value Ascending, i.<NomFichier>.Value Ascending
                Else
                    Return From i In Liste
                                Order By i.<Titre>.Value Descending, i.<NomFichier>.Value Ascending
                End If
            Case "Compositeur"
                If SensColonneTriEnCours = ListSortDirection.Ascending Then
                    Return From i In Liste
                                 Order By i.<Compositeur>.Value Ascending, i.<NomFichier>.Value Ascending
                Else
                    Return From i In Liste
                                Order By i.<Compositeur>.Value Descending, i.<NomFichier>.Value Ascending
                End If
            Case "Catalogue"
                If SensColonneTriEnCours = ListSortDirection.Ascending Then
                    Return From i In Liste
                                 Order By i.<Catalogue>.Value Ascending, i.<NomFichier>.Value Ascending
                Else
                    Return From i In Liste
                                Order By i.<Catalogue>.Value Descending, i.<NomFichier>.Value Ascending
                End If
            Case "Label"
                If SensColonneTriEnCours = ListSortDirection.Ascending Then
                    Return From i In Liste
                                 Order By i.<Label>.Value Ascending, i.<NomFichier>.Value Ascending
                Else
                    Return From i In Liste
                                Order By i.<Label>.Value Descending, i.<NomFichier>.Value Ascending
                End If
            Case "Annee"
                If SensColonneTriEnCours = ListSortDirection.Ascending Then
                    Return From i In Liste
                                 Order By i.<Annee>.Value Ascending, i.<NomFichier>.Value Ascending
                Else
                    Return From i In Liste
                                Order By i.<Annee>.Value Descending, i.<NomFichier>.Value Ascending
                End If
            Case "Style"
                If SensColonneTriEnCours = ListSortDirection.Ascending Then
                    Return From i In Liste
                                 Order By i.<Style>.Value Ascending, i.<NomFichier>.Value Ascending
                Else
                    Return From i In Liste
                                Order By i.<Style>.Value Descending, i.<NomFichier>.Value Ascending
                End If
            Case "Repertoire"
                If SensColonneTriEnCours = ListSortDirection.Ascending Then
                    Return From i In Liste
                                 Order By i.<Repertoire>.Value Ascending, i.<NomFichier>.Value Ascending
                Else
                    Return From i In Liste
                                Order By i.<Repertoire>.Value Descending, i.<NomFichier>.Value Ascending
                End If
            Case "Album"
                If SensColonneTriEnCours = ListSortDirection.Ascending Then
                    Return From i In Liste
                                 Order By i.<Album>.Value Ascending, i.<NomFichier>.Value Ascending
                Else
                    Return From i In Liste
                                Order By i.<Album>.Value Descending, i.<NomFichier>.Value Ascending
                End If
            Case "Taille"
                If SensColonneTriEnCours = ListSortDirection.Ascending Then
                    Return From i In Liste
                                 Order By i.<Taille>.Value Ascending, i.<NomFichier>.Value Ascending
                Else
                    Return From i In Liste
                                Order By i.<Taille>.Value Descending, i.<NomFichier>.Value Ascending
                End If
            Case "Piste"
                If SensColonneTriEnCours = ListSortDirection.Ascending Then
                    Return From i In Liste
                                 Order By i.<Piste>.Value Ascending, i.<NomFichier>.Value Ascending
                Else
                    Return From i In Liste
                                Order By i.<Piste>.Value Descending, i.<NomFichier>.Value Ascending
                End If
            Case Else
                Return Liste
        End Select

    End Function

End Class

