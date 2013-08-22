Option Compare Text
'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.1
'DATE : 13/11/10
'DESCRIPTION :Controle treeView avec repertoire
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System
Imports System.IO
Imports System.String
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.Devices
Imports System.ComponentModel
Imports DragDropLib
Imports gbDev
Imports DataObject = System.Windows.DataObject
Imports System.Threading
Imports System.Windows.Threading

Public Class userControlDirectoriesList
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES EVENEMENTS DU CONTROLE
    Event SelectionChanged(ByVal Path As String)
    Event BeforeUpdate(ByVal TypeOperation As String, ByVal Path As String, ByRef Annuler As Boolean)
    Event AfterUpdate(ByVal TypeOperation As String, ByVal NewPath As String)
    Event RequeteConvertFile(ByVal TabFichiers() As String, ByVal Destination As String, ByVal typeConversion As fileConverter.ConvertType)

    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES VARIABLES DU CONTROLE
    Dim Racine As String = "E:\"                    'Racine de la liste
    Dim ListeIsInit As Boolean = False              'Liste des répertoires valide
    Dim ActionCopierEnCours As Boolean
    Dim ActionCreationRepEnCours As Boolean
    Dim PlateformVista As Boolean
    Dim WithEvents ShellWatcher As New System.IO.FileSystemWatcher
    Dim WithEvents TimerEditLabel As New Timers.Timer
    Dim ToucheDown As Boolean                         'Indique si une touche clavier est enfoncée
    Private Delegate Sub NoArgDelegate()
    'PROPRIETE DE PERSONNALISATION COMPORTEMENT
    Public Property ForceCopyFiles As Boolean
    Public Property ExecuteFileDropAction As DelegateSpecificDropAction
    Public Property ExecuteDirectoryDropAction As DelegateSpecificDropAction
    Public Delegate Sub DelegateSpecificDropAction(ByVal Name As String)

    '***********************************************************************************************
    '---------------------------------CONSTRUCTEUR DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Public Sub New()
        ' Cet appel est requis par le Concepteur Windows Form.
        InitializeComponent()
        TimerEditLabel.Enabled = False
        TimerEditLabel.Interval = 1000
        gbListe.AddHandler(TreeViewItem.ExpandedEvent, New RoutedEventHandler(AddressOf TreeViewItemExpanded))
        gbListe.AddHandler(TreeViewItem.CollapsedEvent, New RoutedEventHandler(AddressOf TreeViewItemCollapsed))
        gbListe.AllowDrop = True
        Racine = gbRacine
        If System.Environment.OSVersion.Platform = PlatformID.Win32NT Then
            If System.Environment.OSVersion.Version.Major > 5 Then PlateformVista = True
        End If
        InitListe()
    End Sub
    '***********************************************************************************************
    '----------------------------------DESTRUCTEUR DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Protected Overrides Sub Finalize()
        gbListe.RemoveHandler(TreeViewItem.ExpandedEvent, New RoutedEventHandler(AddressOf TreeViewItemExpanded))
        gbListe.RemoveHandler(TreeViewItem.CollapsedEvent, New RoutedEventHandler(AddressOf TreeViewItemCollapsed))
        MyBase.Finalize()
    End Sub
    '***********************************************************************************************
    '----------------------------PROPRIETES DE LA CLASSE EN LECTURE/ECRITURE------------------------
    '***********************************************************************************************
    'DECLARATION DES PROPRIETES DE LA CLASSE
    Public Property gbRacine() As String
        Get
            Return Racine
        End Get
        Set(ByVal value As String)
            If value <> "" Then
                Racine = Strings.UCase(value)
                If Path.GetPathRoot(Racine) <> Racine Then
                    If Right(Racine, 1) = "\" Then Racine = Left(Racine, Racine.Length - 1)
                End If
                ListeIsInit = False
                InitListe()
            End If
        End Set
    End Property
    Public Property gbFolderSelected() As String
        Get
            Try
                If Not gbListe.SelectedItem Is Nothing Then
                    Return GetItemPath(gbListe.SelectedItem)
                Else
                    Return gbRacine
                End If
            Catch ex As Exception
            End Try
            Return ""
        End Get
        Set(ByVal value As String)
            If value <> gbRacine Then SelectNode(value) Else SelectNode("")
        End Set
    End Property

    '***********************************************************************************************
    '-------------------------------METHODES PUBLICS DE LA CLASSE-----------------------------------
    '***********************************************************************************************        
    'FONCTION DE CREATION D'UN NOUVEAU REPERTOIRE DANS LE REPERTOIRE SELECTIONNE
    Public Function NewDirectory(ByVal NomRepertoire As String) As String
        Dim Annulation As Boolean = False
        Dim NomDirectory As String = ""
        Dim NomRepRacine As String
        If gbFolderSelected <> "" Then NomRepRacine = gbFolderSelected Else NomRepRacine = gbRacine
        Try
            If NomRepertoire <> "" Then
                NomDirectory = NomRepertoire
                Do Until Not My.Computer.FileSystem.DirectoryExists(NomRepRacine & "\" & NomDirectory)
                    NomDirectory = "_" & NomDirectory
                Loop
                RaiseEvent BeforeUpdate("CreateDirectory", NomRepRacine & "\" & NomDirectory, Annulation)
                If Not Annulation Then
                    My.Computer.FileSystem.CreateDirectory(NomRepRacine & "\" & NomDirectory)
                    RaiseEvent AfterUpdate("CreateDirectory", NomRepRacine & "\" & NomDirectory)
                    ActionCreationRepEnCours = True
                    Return NomRepRacine & "\" & NomDirectory
                End If
            End If
        Catch ex As Exception
            Debug.Print("Erreur lors de la creation d'un repertoire : " & NomRepRacine & "\" & NomDirectory)
        End Try
        RaiseEvent AfterUpdate("CancelCreateDirectory", NomRepRacine & "\" & NomDirectory)
        Return ""
    End Function

    'FONCTION POUR RENOMMER UN REPERTOIRE
    Friend Function DirectoryRename(ByVal Item As TreeViewItem, ByVal NewName As String, ByVal OldName As String) As Boolean
        Dim Annulation As Boolean = False
        Dim NomRepRacine As String = Path.GetDirectoryName(GetItemPath(Item))
        Try
            If NewName <> "" And Item.Header <> OldName Then
                RaiseEvent BeforeUpdate("RenameDirectory", NomRepRacine & "\" & NewName, Annulation)
                If Not Annulation Then
                    My.Computer.FileSystem.RenameDirectory(NomRepRacine & "\" & OldName, NewName)
                    RaiseEvent AfterUpdate("RenameDirectory", NomRepRacine & "\" & NewName)
                    If Item.IsSelected Then RaiseEvent SelectionChanged(gbFolderSelected)
                    Return True
                End If
            End If
        Catch ex As Exception
            wpfMsgBox.MsgBoxInfo("Opération annulée", ex.Message, , "Impossible de renommer le répertoire avec ce nom")
            ' MsgBox(ex.Message, MsgBoxStyle.Critical Or MsgBoxStyle.OkOnly, "Opération annulée")
        End Try
        RaiseEvent AfterUpdate("CancelRenameDirectory", NomRepRacine & "\" & OldName)
        Return False
    End Function

    'FONCTION POUR EFFACER UN REPERTOIRE
    Private Function DirectoryDelete(ByVal FullName As String) As Boolean
        If gbFolderSelected IsNot Nothing Then
            Dim Annulation As Boolean = False
            RaiseEvent BeforeUpdate("DeleteDirectory", FullName, Annulation)
            Try
                If Not Annulation Then
                    My.Computer.FileSystem.DeleteDirectory(FullName, Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, FileIO.RecycleOption.SendToRecycleBin, FileIO.UICancelOption.ThrowException)
                    RaiseEvent AfterUpdate("DeleteDirectory", FullName)
                    gbListe.Focus()
                    Return True
                End If
            Catch ex As Exception
            End Try
            RaiseEvent AfterUpdate("CancelDeleteDirectory", FullName)
        End If
        Return (False)
    End Function

    'FONCTION DE TRAITEMENT DU DROP SUR LA LISTE
    Friend Sub DirectoryDrop(ByVal e As System.Windows.DragEventArgs, Optional ByVal Item As TreeViewItem = Nothing)
        Dim Formats As String() = e.Data.GetFormats()
        Dim Chemin As String = ""
        Dim NomDirectory As String = ""
        Dim NomFichier As String = ""
        ' For Each f In e.Data.GetFormats
        ' Debug.Print(f)
        ' Debug.Print(e.Data.GetData(f).GetType.ToString())
        ' Next
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            Dim Info As Object = e.Data.GetData(DataFormats.FileDrop)
            For Each j In CType(Info, String())
                Try
                    Dim Annulation As Boolean
                    If Item IsNot Nothing Then
                        Chemin = GetItemPath(Item)
                    Else
                        Chemin = gbRacine
                    End If
                    If FileIO.FileSystem.DirectoryExists(j) Then
                        NomDirectory = Path.GetFileName(j)
                        If ((e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey) Or ForceCopyFiles Then 'COPY DIRECTORY
                            Do Until Not My.Computer.FileSystem.DirectoryExists(Chemin & "\" & NomDirectory)
                                NomDirectory = "Copie de " & NomDirectory
                            Loop
                            RaiseEvent BeforeUpdate("CopyDirectory", j, Annulation)
                            If Not Annulation Then
                                My.Computer.FileSystem.CopyDirectory(j, Chemin & "\" & NomDirectory, FileIO.UIOption.AllDialogs, FileIO.UICancelOption.ThrowException)
                                If ExecuteDirectoryDropAction IsNot Nothing Then
                                    Dim FunctionAExecuter As DelegateSpecificDropAction = ExecuteDirectoryDropAction
                                    FunctionAExecuter(Chemin & "\" & NomDirectory)
                                End If
                                RaiseEvent AfterUpdate("CopyDirectory", Chemin & "\" & NomDirectory)
                            End If
                        Else 'MOVE DIRECTORY
                            RaiseEvent BeforeUpdate("MoveDirectory", j, Annulation)
                            If Not Annulation Then
                                My.Computer.FileSystem.MoveDirectory(j, Chemin & "\" & NomDirectory, FileIO.UIOption.AllDialogs, FileIO.UICancelOption.ThrowException)
                                RaiseEvent AfterUpdate("MoveDirectory", Chemin & "\" & NomDirectory)
                            End If
                        End If
                    ElseIf FileIO.FileSystem.FileExists(j) Then
                        NomFichier = Path.GetFileName(j)
                        If ((e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey) Or ForceCopyFiles Then 'COPY FILE
                            Do Until Not My.Computer.FileSystem.FileExists(Chemin & "\" & NomFichier)
                                NomFichier = "Copie de " & NomFichier
                            Loop
                            RaiseEvent BeforeUpdate("CopyFile", j, Annulation)
                            If Not Annulation Then
                                My.Computer.FileSystem.CopyFile(j, Chemin & "\" & NomFichier, FileIO.UIOption.AllDialogs, FileIO.UICancelOption.ThrowException)
                                If ExecuteFileDropAction IsNot Nothing Then
                                    Dim FunctionAExecuter As DelegateSpecificDropAction = ExecuteFileDropAction
                                    Me.Dispatcher.BeginInvoke(Sub(FichierATraiter As String)
                                                                  FunctionAExecuter(FichierATraiter)
                                                              End Sub, DispatcherPriority.Background, {Chemin & "\" & NomFichier})
                                End If
                                RaiseEvent AfterUpdate("CopyFile", Chemin & "\" & NomFichier)
                            End If
                        Else 'MOVE FILE
                            RaiseEvent BeforeUpdate("MoveFile", j, Annulation)
                            If Not Annulation Then
                                My.Computer.FileSystem.MoveFile(j, Chemin & "\" & NomFichier, FileIO.UIOption.AllDialogs, FileIO.UICancelOption.ThrowException)
                                RaiseEvent AfterUpdate("MoveFile", Chemin & "\" & NomFichier)
                            End If
                        End If
                    End If
                Catch ex As Exception
                    If (e.KeyStates And 8) = 8 Then
                        If NomDirectory <> "" Then
                            RaiseEvent AfterUpdate("CancelCopyDirectory", j)
                        ElseIf NomFichier <> "" Then
                            RaiseEvent AfterUpdate("CancelCopyFile", j)
                        End If
                    Else
                        If NomDirectory <> "" Then
                            RaiseEvent AfterUpdate("CancelMoveDirectory", j)
                        ElseIf NomFichier <> "" Then
                            RaiseEvent AfterUpdate("CancelMoveFile", j)
                        End If
                    End If
                End Try
            Next
        End If
    End Sub
    Friend Sub FileCDAudioDrop(ByVal e As System.Windows.DragEventArgs, Optional ByVal Item As TreeViewItem = Nothing,
                               Optional ByVal typeConversion As fileConverter.ConvertType = fileConverter.ConvertType.mp3)
        Dim Chemin As String
        If e.Data.GetDataPresent("FileCDAudio") Then
            If Item IsNot Nothing Then
                Chemin = GetItemPath(Item)
            Else
                Chemin = gbRacine
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

    '***********************************************************************************************
    '-------------------------------METHODES PRIVEES DE LA CLASSE-----------------------------------
    '***********************************************************************************************        
    'METHODES D'INITIALISATION DE LA LISTE
    Private Sub InitListe()
        If Not Directory.Exists(Racine) Then
            gbListe.Items.Clear()
            gbListe.Items.Add("Chemin d'accès du dossier racine non valide...")
            '            Liste.SelectedImageIndex = 2
        Else
            If Not ListeIsInit Then
                InitDirectoryList(Racine)
                gbListe.IsEnabled = True
                'Liste.SelectedImageIndex = 0
                ShellWatcher.IncludeSubdirectories = True
                ShellWatcher.Path = gbRacine
                ShellWatcher.NotifyFilter = NotifyFilters.DirectoryName
                ShellWatcher.EnableRaisingEvents = True
                ListeIsInit = True
            End If
        End If
    End Sub
    Private Sub InitDirectoryList(ByVal Chemin As String, Optional ByVal ItemsNodeParent As ItemCollection = Nothing)
        Try
            Dim DirList() As String = Directory.GetDirectories(Chemin & "\")
            If ItemsNodeParent Is Nothing Then ItemsNodeParent = gbListe.Items
            ItemsNodeParent.Clear()
            Array.ForEach(DirList, Sub(i As String)
                                       If Not (FileIO.FileSystem.GetDirectoryInfo(i).Attributes And IO.FileAttributes.System) = IO.FileAttributes.System And
                                          (FileIO.FileSystem.GetDirectoryInfo(i).Attributes And IO.FileAttributes.Directory) = IO.FileAttributes.Directory Then
                                           MiseAJourList(i, ItemsNodeParent)
                                       End If
                                   End Sub)
            ItemsNodeParent.SortDescriptions.Clear()
            ItemsNodeParent.SortDescriptions.Add(New SortDescription("Header", ListSortDirection.Ascending))
            ItemsNodeParent.Refresh()
        Catch ex As Exception
            Debug.Print(ex.Message)
        End Try
        Return
    End Sub
    Private Sub MiseAJourList(ByVal FullName As String, ByVal ItemsNodeParent As ItemCollection)
        Try
            Dim NewItem As TreeViewItem = New gbDirectoryItem(FullName, Me, ItemsNodeParent)
            Dim SDirList() As String = Directory.GetDirectories(FullName)
            ItemsNodeParent.Add(NewItem)
            If SDirList.Count > 0 Then NewItem.Items.Add(New gbDirectoryItem("xxxxxxxxxx", Me, NewItem.Items))
        Catch ex As Exception
        End Try
    End Sub

    'METHODES POUR LA GESTION DE LA SELECTION
    Delegate Sub CallBackToBringIntoView(ByVal e As TreeViewItem)
    Private Sub SelectNode(ByVal Name As String)
        If Name = "" Then
            Dim ItemEnCours As TreeViewItem = gbListe.SelectedItem
            If ItemEnCours IsNot Nothing Then ItemEnCours.IsSelected = False
        Else
            Dim Chaine As String
            If Right(Racine, 1) = "\" Then
                Chaine = Texte.RemplaceChaine(Name, Racine, "")
            Else
                Chaine = Texte.RemplaceChaine(Name, Racine & "\", "")
            End If
            Dim ItemSup As TreeViewItem = Nothing
            If Right(Chaine, 1) = "\" Then Chaine = Left(Chaine, Chaine.Length - 1)
            Try
                If Directory.Exists(Name) Then
                    Dim ListeRep As String() = Chaine.Split("\")
                    Chaine = Racine
                    If ListeRep.Count > 0 Then
                        For i As Integer = 0 To ListeRep.Count - 1
                            Chaine = Chaine & "\" & ListeRep(i).ToString
                            If ItemSup Is Nothing Then
                                ItemSup = GetItem(gbListe.Items, Path.GetFileName(Chaine))
                            Else
                                ItemSup = GetItem(ItemSup.Items, Path.GetFileName(Chaine))
                            End If
                            If i = ListeRep.Count - 1 Then
                                ItemSup.IsSelected = True
                                Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                                          New CallBackToBringIntoView(AddressOf NotifyBringIntoView), ItemSup)
                            Else
                                If ItemSup IsNot Nothing Then
                                    If Not ItemSup.IsExpanded Then
                                        ItemSup.IsExpanded = True
                                    End If
                                End If
                            End If
                        Next i
                    End If
                End If
            Catch ex As Exception
                wpfMsgBox.MsgBoxInfo("Erreur gbDirectory", ex.Message, Nothing)
                ' MsgBox(ex.Message)
            End Try
        End If
    End Sub
    Private Sub NotifyBringIntoView(ByVal e As TreeViewItem)
        e.BringIntoView()
    End Sub

    'Traitement du clic de la souris hors item pour traiter la désélection
    Private Sub gbListe_MouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles gbListe.MouseLeftButtonDown
        gbFolderSelected = gbRacine
    End Sub

    'TRAITEMENT DES MESSAGES POUR MISE A JOUR LISTE
    'Traitement du message lors d'un changement de sélection
    Private Sub gbListe_SelectedItemChanged(ByVal sender As Object, ByVal e As System.Windows.RoutedPropertyChangedEventArgs(Of Object)) Handles gbListe.SelectedItemChanged
        RaiseEvent SelectionChanged(gbFolderSelected)
    End Sub
    'Traitement du message lors de l'appui sur une touche
    Private Sub gbListe_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Input.KeyEventArgs) Handles gbListe.KeyDown
        Select Case e.Key
            Case Key.Delete
                DirectoryDelete(gbFolderSelected)
        End Select
    End Sub
    Private Sub gbListe_PreviewKeyDown(ByVal sender As Object, ByVal e As System.Windows.Input.KeyEventArgs) Handles gbListe.PreviewKeyDown
        Debug.Print(e.Key.ToString)
        Select Case e.Key
            Case Key.Up
                If gbListe.SelectedItem Is Nothing Then Return
                Dim ItemActif As gbDirectoryItem = CType(gbListe.SelectedItem, gbDirectoryItem)
                Dim IndexItemActif As Integer = ItemActif.ItemsNodeParent.IndexOf(gbListe.SelectedItem)
                If IndexItemActif = 0 Then
                    Dim ItemParent As gbDirectoryItem = GetNodeIsVisible(Path.GetDirectoryName(ItemActif.Tag))
                    If ItemParent IsNot Nothing Then ItemParent.IsSelected = True
                Else
                    Dim ItemPrecedent As gbDirectoryItem = CType(ItemActif.ItemsNodeParent.Item(IndexItemActif - 1), gbDirectoryItem)
                    Do While True
                        If Not ItemPrecedent.IsExpanded Then
                            ItemPrecedent.IsSelected = True
                            Exit Do
                        End If
                        ItemPrecedent.Items.MoveCurrentToLast()
                        Dim ItemLast As gbDirectoryItem
                        If ItemPrecedent.Items.Count = 0 Then
                            ItemLast = ItemPrecedent
                        Else
                            ItemLast = ItemPrecedent.Items.CurrentItem
                        End If
                        If (ItemLast.IsExpanded And ItemLast.HasItems) Then
                            ItemPrecedent = ItemLast
                        Else
                            ItemLast.IsSelected = True
                            Exit Do
                        End If
                    Loop
                End If
                e.Handled = True
            Case Key.Down
                If gbListe.SelectedItem Is Nothing Then Return
                Dim ItemActif As gbDirectoryItem = CType(gbListe.SelectedItem, gbDirectoryItem)
                If ItemActif.IsExpanded And ItemActif.HasItems Then
                    CType(ItemActif.Items(0), gbDirectoryItem).IsSelected = True
                Else
                    Dim IndexItemActif As Integer = ItemActif.ItemsNodeParent.IndexOf(gbListe.SelectedItem)
                    If IndexItemActif < ItemActif.ItemsNodeParent.Count - 1 Then
                        CType(ItemActif.ItemsNodeParent.Item(IndexItemActif + 1), gbDirectoryItem).IsSelected = True
                    Else
                        Do While True
                            Dim ItemParent As gbDirectoryItem = GetNodeIsVisible(Path.GetDirectoryName(ItemActif.Tag))
                            If ItemParent Is Nothing Then Exit Do
                            Dim IndexItemParent As Integer = ItemParent.ItemsNodeParent.IndexOf(ItemParent)
                            If IndexItemParent < ItemParent.ItemsNodeParent.Count - 1 Then
                                CType(ItemParent.ItemsNodeParent.Item(IndexItemParent + 1), gbDirectoryItem).IsSelected = True
                                Exit Do
                            Else
                                ItemActif = ItemParent
                            End If
                        Loop
                    End If
                End If
                e.Handled = True
        End Select
    End Sub
    'Traitement du message lors d'une expansion d'un item
    Private Sub TreeViewItemExpanded(ByVal sender As Object, ByVal e As RoutedEventArgs)
        Dim Item As TreeViewItem = CType(e.Source, TreeViewItem)
        If Item IsNot Nothing Then
            '  Thread.Sleep(10000)
            Me.Cursor = Cursors.Wait
            InitDirectoryList(GetItemPath(Item), Item.Items)
            Me.Cursor = Cursors.Arrow
        End If
    End Sub
    'Traitement du message lors de la réduction d'un item
    Private Sub TreeViewItemCollapsed(ByVal sender As Object, ByVal e As RoutedEventArgs)

    End Sub

    'Traitement du message lors de la notification de creation d'un repertoire
    Delegate Sub CallBackToFileSystemWatcher(ByVal e As FileSystemEventArgs)
    Private Sub ShellWatcher_Created(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles ShellWatcher.Created
        'Pour regler le probleme de thread, invocation d'une fonction delegée
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, New CallBackToFileSystemWatcher(AddressOf NotifyShellWatcherCreated), e)
    End Sub
    Private Sub NotifyShellWatcherCreated(ByVal e As FileSystemEventArgs)
        Dim ListeNodes As ItemCollection = GetNodesIsVisible(Path.GetDirectoryName(e.FullPath))
        If ListeNodes IsNot Nothing Then
            MiseAJourList(e.FullPath, ListeNodes)
            ListeNodes.SortDescriptions.Clear()
            ListeNodes.SortDescriptions.Add(New SortDescription("Header", ListSortDirection.Ascending))
            ListeNodes.Refresh()
            If ActionCreationRepEnCours Then
                gbFolderSelected = e.FullPath
                ActionCreationRepEnCours = False
            End If
        End If
    End Sub
    'Traitement du message lors de la notification d'effacement d'un repertoire
    Private Sub ShellWatcher_Deleted(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles ShellWatcher.Deleted
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, New CallBackToFileSystemWatcher(AddressOf NotifyShellWatcherDeleted), e)
    End Sub
    Private Sub NotifyShellWatcherDeleted(ByVal e As FileSystemEventArgs)
        Dim Nodes As ItemCollection = GetNodesIsExpanded(Path.GetDirectoryName(e.FullPath))
        If Nodes IsNot Nothing Then Nodes.Remove(GetItem(Nodes, e.FullPath))
    End Sub
    'Traitement du message lors de la notification de renommage d'un repertoire
    Delegate Sub CallBackToFileSystemWatcherRenamed(ByVal e As System.IO.RenamedEventArgs)
    Private Sub ShellWatcher_Renamed(ByVal sender As Object, ByVal e As System.IO.RenamedEventArgs) Handles ShellWatcher.Renamed
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                  New CallBackToFileSystemWatcherRenamed(AddressOf NotifyShellWatcherRenamed), e)
    End Sub
    Private Sub NotifyShellWatcherRenamed(ByVal e As System.IO.RenamedEventArgs)
        Dim Node As TreeViewItem = GetNodeIsVisible(e.OldFullPath)
        If Node IsNot Nothing Then Node.Header = Path.GetFileName(e.FullPath)
    End Sub

    'Traitement du message lors d'un drop sur la liste hors item
    Private Sub gbListe_Drop(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles gbListe.Drop
        If (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
            If ((e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey) Or ForceCopyFiles Then _
                e.Effects = e.AllowedEffects And DragDropEffects.Copy _
                Else e.Effects = e.AllowedEffects And DragDropEffects.Move
            If ReferenceEquals(e.Source, gbListe) Then DirectoryDrop(e)
        ElseIf (e.Data.GetDataPresent("fileCDAudio")) Then
            If Directory.Exists(gbRacine) Then
                If (e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey Then
                    If ReferenceEquals(e.Source, gbListe) Then FileCDAudioDrop(e, , fileConverter.ConvertType.wav)
                Else
                    If ReferenceEquals(e.Source, gbListe) Then FileCDAudioDrop(e, , fileConverter.ConvertType.mp3)
                End If
            End If
        Else
            e.Effects = DragDropEffects.None
        End If
        If PlateformVista Then DropTargetHelper.Drop(e.Data, e.GetPosition(Me), e.Effects)
        e.Handled = True
    End Sub
    'Traitement du message lors d'un drap entre dans la liste hors item
    Private Sub gbListe_DragEnter(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles gbListe.DragEnter
        Dim FlagPlateformVista As Boolean
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        If (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
            If ((e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey) Or ForceCopyFiles Then
                e.Effects = e.AllowedEffects And DragDropEffects.Copy
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me), e.Data, e.GetPosition(Me), e.Effects, "Copier vers %1", gbRacine)
            Else
                e.Effects = e.AllowedEffects And DragDropEffects.Move
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me), e.Data, e.GetPosition(Me), e.Effects, "Déplace vers %1", gbRacine) ' CType(Me.Template.FindName("GBTextBlock", Me), textblock).he)
            End If
        Else
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me),
                                                            e.Data, e.GetPosition(Me),
                                                            e.Effects, "Copie impossible sur ce type de sélection", "")
            e.Effects = DragDropEffects.None
        End If
        e.Handled = True

    End Sub
    'Traitement du message lors d'un drap sort dans la liste hors item
    Private Sub gbListe_DragLeave(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles gbListe.DragLeave
        'If (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
        If PlateformVista Then DropTargetHelper.DragLeave(e.Data)
        e.Handled = True
        ' End If

    End Sub
    'Traitement du message lors d'un drap survole la liste hors item
    Private Sub gbListe_DragOver(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles gbListe.DragOver
        'PERMET DE SCROLLER AUTOMATIQUEMENT LA LISTE
        If e.GetPosition(gbListe).Y < 20 Then
            CType(gbListe.Template.FindName("GBScrollViewer", gbListe), ScrollViewer).LineUp()
        End If
        If e.GetPosition(gbListe).Y > (gbListe.ActualHeight - 20) Then
            CType(gbListe.Template.FindName("GBScrollViewer", gbListe), ScrollViewer).LineDown()
        End If
        'GESTION DU DRAG & DROP
        Dim FlagPlateformVista As Boolean
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        If (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
            If ((e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey) Or ForceCopyFiles Then
                e.Effects = e.AllowedEffects And DragDropEffects.Copy
                If Not ActionCopierEnCours Then
                    If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me), e.Data, e.GetPosition(Me), e.Effects, "Copier vers %1", gbRacine)
                    ActionCopierEnCours = True
                Else
                    If PlateformVista Then DropTargetHelper.DragOver(e.GetPosition(Me), e.Effects)
                End If
            Else
                e.Effects = e.AllowedEffects And DragDropEffects.Move
                If ActionCopierEnCours Then
                    If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me), e.Data, e.GetPosition(Me), e.Effects, "Déplace vers %1", gbRacine) ' CType(Me.Template.FindName("GBTextBlock", Me), textblock).he)
                    ActionCopierEnCours = False
                Else
                    If PlateformVista Then DropTargetHelper.DragOver(e.GetPosition(Me), e.Effects)
                End If
            End If
        ElseIf (e.Data.GetDataPresent("fileCDAudio")) Then
            If Directory.Exists(gbRacine) Then
                e.Effects = e.AllowedEffects And DragDropEffects.Copy
                If (e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey Then
                    If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me),
                                                                            e.Data, e.GetPosition(Me),
                                                                            e.Effects, "Copier vers %1", gbRacine)
                Else
                    If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me),
                                                                  e.Data, e.GetPosition(Me),
                                                                  e.Effects, "Convertir en mp3 vers %1", gbRacine)
                End If
            Else
                e.Effects = DragDropEffects.None
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me), e.Data, e.GetPosition(Me), e.Effects, "Opération imposible sur %1", gbRacine)
            End If
        Else
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me),
                                                            e.Data, e.GetPosition(Me),
                                                            e.Effects, "Copie impossible sur ce type de sélection", "")
            e.Effects = DragDropEffects.None
        End If
        e.Handled = True
    End Sub

    'METHODES UTILITAIRES
    Private Function GetItem(ByVal ItemsNodeParent As ItemCollection, ByVal FullPath As String) As TreeViewItem
        If ItemsNodeParent IsNot Nothing And FullPath <> "" Then
            For Each Item As TreeViewItem In ItemsNodeParent
                '                If UCase(Item.Header) = UCase(Path.GetFileName(FullPath)) Then
                If Item.Header = Path.GetFileName(FullPath) Then
                    Return Item
                End If
            Next
        End If
        Return Nothing
    End Function
    Friend Function GetItemPath(ByVal Item As TreeViewItem) As String
        If TypeOf Item.Parent Is TreeViewItem Then
            Return GetItemPath(Item.Parent) & "\" & Item.Header
        Else
            If Right(Racine, 1) = "\" Then
                Return Racine & Item.Header
            Else
                Return Racine & "\" & Item.Header
            End If
        End If
    End Function
    Private Function GetNodesIsExpanded(ByVal FullPath As String) As ItemCollection
        Dim ListeNodes As ItemCollection
        Dim Item As TreeViewItem
        'If UCase(FullPath) = UCase(Racine) Then
        If FullPath = Racine Then
            Return gbListe.Items
        Else
            ListeNodes = GetNodesIsExpanded(Path.GetDirectoryName(FullPath))
            Item = GetItem(ListeNodes, FullPath)
            If Item IsNot Nothing Then
                If GetItem(ListeNodes, FullPath).IsExpanded Then
                    Return GetItem(ListeNodes, FullPath).Items
                End If
            End If
        End If
        Return Nothing
    End Function
    Private Function GetNodesIsVisible(ByVal FullPath As String) As ItemCollection
        Dim ListeNodes As ItemCollection
        Dim Item As TreeViewItem
        ' If UCase(FullPath) = UCase(Racine) Then
        If FullPath Is Nothing Then Return Nothing
        If FullPath = Racine Then
            Return gbListe.Items
        Else
            ListeNodes = GetNodesIsVisible(Path.GetDirectoryName(FullPath))
            Item = GetItem(ListeNodes, FullPath)
            If Item IsNot Nothing Then
                Return GetItem(ListeNodes, FullPath).Items
            End If
        End If
        Return Nothing
    End Function
    Private Function GetNodeIsVisible(ByVal FullPath As String) As TreeViewItem
        Return GetItem(GetNodesIsVisible(Path.GetDirectoryName(FullPath)), FullPath)
    End Function

    'METHODES DE REPONSE AUX MENUS CONTEXTUELS
    Private Sub CreationRepertoire_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        NewDirectory("Nouveau")
    End Sub
    Private Sub SuppressionRepertoire_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        DirectoryDelete(gbFolderSelected)
    End Sub
End Class

Friend Class gbDirectoryItem
    Inherits TreeViewItem
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Dim ListeParente As userControlDirectoriesList         'Stocke la liste parente de l'item
    Dim ImageDragDrop As Image
    Public ItemsNodeParent As ItemCollection   'Stocke la collection parente de l'item
    Dim EditLabelPossible As Boolean        'Signale si le label sera éditable au prochain clic
    Dim LabelBeforeEdit As String           'Stocke le label avant son édition
    Dim WithEvents TimerEditLabel As New Timers.Timer   'Timer pour tempo avant deuxieme clic d'edit
    Dim WithEvents TimerExpandeItem As New Timers.Timer   '
    Dim CouleurBordure As Brush
    Dim StartPoint As Point
    Dim ActionCopierEnCours As Boolean
    Dim PlateformVista As Boolean

    '***********************************************************************************************
    '---------------------------------CONSTRUCTEUR DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    'Creation d'un item dans le DirectoryTreeView
    Public Sub New(ByVal NomItem As String, ByVal Liste As userControlDirectoriesList, ByVal ItemsParent As ItemCollection)
        If Liste IsNot Nothing Then
            ItemsNodeParent = ItemsParent
            ListeParente = Liste
            Tag = NomItem
            Header = Path.GetFileName(NomItem)
            Style = CType(Liste.FindResource("GBItems"), Style)
            If System.Environment.OSVersion.Platform = PlatformID.Win32NT Then
                If System.Environment.OSVersion.Version.Major > 5 Then PlateformVista = True
            End If
        End If
    End Sub

    '***********************************************************************************************
    '-------------------------------METHODES PRIVEES DE LA CLASSE-----------------------------------
    '***********************************************************************************************        
    'TRAITEMENT DES MESSAGES POUR MISE A JOUR DE L'ETAT DE L'TEM
    'Traitement du message lors d'un clic sur le bouton gauche de la souris
    Private Sub gbDirectoryItem_PreviewMouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonDown
        StartPoint = e.GetPosition(Me)
        If TypeOf (e.OriginalSource) Is TextBlock Then
            If ReferenceEquals(e.Source, Me) Then
                TimerEditLabel.Interval = 700
                TimerEditLabel.Enabled = True
                If Not (TryCast(e.OriginalSource, TextBlock).Text <> Header) And EditLabelPossible Then
                    e.Handled = True
                    CType(Me.Template.FindName("GBTextBlockItem", Me), TextBlock).Visibility = Windows.Visibility.Hidden
                    ' CType(Me.Template.FindName("GBTextBlockItem2", Me), TextBlock).Visibility = Windows.Visibility.Hidden
                    CType(Me.Template.FindName("GBTextBoxItem", Me), TextBox).Visibility = Windows.Visibility.Visible
                    CType(Me.Template.FindName("GBTextBoxItem", Me), TextBox).Focus()
                    LabelBeforeEdit = Header
                End If
            End If
        End If
        EditLabelPossible = False
    End Sub
    Private Sub gbDirectoryItem_PreviewMouseLeftButtonUp(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonUp
        StartPoint = New Point
    End Sub
    Private Sub gbDirectoryItem_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles Me.MouseLeave
        StartPoint = New Point
    End Sub    'Traitement du message lors d'un deplacement de la souris
    Private Sub gbDirectoryItem_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles Me.MouseMove
        If ReferenceEquals(e.Source, Me) And Not LabelBeforeEdit <> "" Then
            Dim MousePos As Point = e.GetPosition(Me)
            Dim Diff As Vector = StartPoint - MousePos
            If StartPoint.X <> 0 And StartPoint.Y <> 0 And (e.LeftButton = MouseButtonState.Pressed And Math.Abs(Diff.X) > 2 Or
                                                            Math.Abs(Diff.Y) > 2) Then
                Dim DirectoryDataObject(0) As String
                DirectoryDataObject(0) = ListeParente.gbFolderSelected
                If PlateformVista Then
                    DragSourceHelper.DoDragDrop(CType(Me.Template.FindName("GBBorder", Me), Border), e.GetPosition(Me), DragDropEffects.Move Or DragDropEffects.Copy,
                                              New KeyValuePair(Of String, Object)("FileDrop", DirectoryDataObject))
                Else
                    Dim data As DataObject = New DataObject()
                    data.SetData("FileDrop", DirectoryDataObject)
                    Dim effects As DragDropEffects = DragDrop.DoDragDrop(Me, data, DragDropEffects.Copy Or DragDropEffects.Move)
                End If
            End If
        End If
    End Sub

    'Traitement du message lors de l'appui sur une touche du clavier
    Private Sub gbDirectoryItem_PreviewKeyDown(ByVal sender As Object, ByVal e As System.Windows.Input.KeyEventArgs) Handles Me.PreviewKeyDown
        If LabelBeforeEdit <> "" Then
            Select Case e.Key
                Case Key.Enter
                    Header = CType(e.OriginalSource, TextBox).Text
                    EndEditItem(True)
                Case Key.Escape
                    Header = CType(e.OriginalSource, TextBox).Text
                    EndEditItem(False)
                Case Key.Delete
                    'e.Handled = True
            End Select
        End If
    End Sub

    'Traitement du message lorsque l'item est selectionne
    Private Sub gbDirectoryItem_Selected(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Selected
        '    TimerEditLabel.Interval = 700
        '    TimerEditLabel.Enabled = True
        e.Handled = True
    End Sub
    'Traitement du message lorsque l'item est deselectionne
    Private Sub gbDirectoryItem_Unselected(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Unselected
        If ReferenceEquals(e.OriginalSource, Me) Then
            EndEditItem(True)
            e.Handled = True
        End If
    End Sub
    'Traitement du message lorsque le textbox d'edition perd le focus
    Private Sub gbDirectoryItem_LostKeyboardFocus(ByVal sender As Object, ByVal e As System.Windows.Input.KeyboardFocusChangedEventArgs) Handles Me.LostKeyboardFocus
        If TypeOf (e.OriginalSource) Is TextBox Then
            Header = CType(e.OriginalSource, TextBox).Text
            EndEditItem(True)
            e.Handled = True
        End If
    End Sub

    'Traitement du message lors d'une expansion d'un item
    Private Sub gbDirectoryItem_Expanded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Expanded
        If IsSelected Then
            If Me.Items.Count = 0 Then Me.IsExpanded = False
            TimerEditLabel.Enabled = False
            EditLabelPossible = False
        End If
    End Sub
    'Traitement du message lors de la réduction d'un item
    Private Sub gbDirectoryItem_Collapsed(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Collapsed
        If IsSelected Then EditLabelPossible = False
    End Sub
    'Traitement de la fin de la tempo du timer
    Private Sub TimerEditLabel_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles TimerEditLabel.Elapsed
        If TimerEditLabel.Enabled Then
            EditLabelPossible = True
            TimerEditLabel.Enabled = False
        End If
    End Sub

    'METHODE APPELEE A LA FIN D'UN EDIT PAR LOSTFOCUS ET UNSELECTED
    Private LockEndEdit As Boolean
    Private Sub EndEditItem(ByVal ValiderModif As Boolean)
        If (LabelBeforeEdit <> "") And (Not LockEndEdit) Then
            LockEndEdit = True
            CType(Me.Template.FindName("GBTextBlockItem", Me), TextBlock).Visibility = Windows.Visibility.Visible
            ' CType(Me.Template.FindName("GBTextBlockItem2", Me), TextBlock).Visibility = Windows.Visibility.Visible
            CType(Me.Template.FindName("GBTextBoxItem", Me), TextBox).Visibility = Windows.Visibility.Hidden
            If ValiderModif Then
                Header = RemplaceOccurences(Header, """", "_")
                If Header <> LabelBeforeEdit Then
                    If ListeParente.DirectoryRename(Me, Header, LabelBeforeEdit) Then
                        ItemsNodeParent.SortDescriptions.Clear()
                        ItemsNodeParent.SortDescriptions.Add(New SortDescription("Header", ListSortDirection.Ascending))
                        ItemsNodeParent.Refresh()
                        Me.BringIntoView()
                    Else
                        Header = LabelBeforeEdit
                    End If
                End If
            Else
                Header = LabelBeforeEdit
            End If
            LabelBeforeEdit = ""
        End If
        LockEndEdit = False
        EditLabelPossible = False
    End Sub

    'TRAITEMENT DU DRAG AND DROP
    'Traitement du message lors d'un drop sur un item
    Private Sub gbDirectoryItem_Drop(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.Drop
        If (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
            If ((e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey) Or ListeParente.ForceCopyFiles Then _
                e.Effects = e.AllowedEffects And DragDropEffects.Copy _
                Else e.Effects = e.AllowedEffects And DragDropEffects.Move
            If ReferenceEquals(e.Source, Me) Then
                CType(Me.Template.FindName("BordureDragDrop", Me), Border).Visibility = Windows.Visibility.Hidden
                ListeParente.DirectoryDrop(e, Me)
            End If
        ElseIf (e.Data.GetDataPresent("fileCDAudio")) Then
            If ReferenceEquals(e.Source, Me) Then
                CType(Me.Template.FindName("BordureDragDrop", Me), Border).Visibility = Windows.Visibility.Hidden
                If (e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey Then
                    ListeParente.FileCDAudioDrop(e, Me, fileConverter.ConvertType.wav)
                Else
                    ListeParente.FileCDAudioDrop(e, Me, fileConverter.ConvertType.mp3)
                End If
            End If
        Else
            e.Effects = DragDropEffects.None
        End If
        If PlateformVista Then DropTargetHelper.Drop(e.Data, e.GetPosition(Me), e.Effects)
        TimerExpandeItem.Enabled = False
        e.Handled = True
    End Sub
    'Traitement du message lorsqu'un drag entre sur un item
    Private Sub gbDirectoryItem_DragEnter(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.DragEnter
        Dim FlagPlateformVista As Boolean
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        If (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
            CType(Me.Template.FindName("BordureDragDrop", Me), Border).Visibility = Windows.Visibility.Visible
            '    CType(Me.Template.FindName("GBBorder", Me), Border).BorderBrush = CType(ListeParente.FindResource("ItemBorderBrush"), SolidColorBrush)
            If ((e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey) Or ListeParente.ForceCopyFiles Then
                e.Effects = e.AllowedEffects And DragDropEffects.Copy
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me), e.Data, e.GetPosition(Me), e.Effects, "Copier vers %1", Header)
            Else
                e.Effects = e.AllowedEffects And DragDropEffects.Move
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me), e.Data, e.GetPosition(Me), e.Effects, "Déplace vers %1", Header)
            End If
        ElseIf (e.Data.GetDataPresent("fileCDAudio")) Then
            CType(Me.Template.FindName("BordureDragDrop", Me), Border).Visibility = Windows.Visibility.Visible
        Else
            e.Effects = DragDropEffects.None
        End If
        TimerExpandeItem.Interval = 800
        TimerExpandeItem.Enabled = True
        e.Handled = True
    End Sub
    'Traitement du message lors d'un drag sort d'un item
    Private Sub gbDirectoryItem_DragLeave(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.DragLeave
        ' If (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
        '  CType(Me.Template.FindName("GBBorder", Me), Border).BorderBrush = CouleurBordure
        CType(Me.Template.FindName("BordureDragDrop", Me), Border).Visibility = Windows.Visibility.Hidden
        If PlateformVista Then DropTargetHelper.DragLeave(e.Data)
        ' End If
        e.Handled = True
        TimerExpandeItem.Enabled = False
    End Sub
    'Traitement du message lors d'un drag survole d'un item
    Private Sub gbDirectoryItem_DragOver(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.DragOver
        Dim FlagPlateformVista As Boolean
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        If (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
            If ((e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey) Or ListeParente.ForceCopyFiles Then
                e.Effects = e.AllowedEffects And DragDropEffects.Copy
                If Not ActionCopierEnCours Then
                    Try
                        If PlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me), e.Data,
                                                                            e.GetPosition(Me), e.Effects,
                                                                            "Copier vers %1", Header)
                    Catch ex As Exception
                    End Try
                    ActionCopierEnCours = True
                Else
                    If PlateformVista Then DropTargetHelper.DragOver(e.GetPosition(Me), e.Effects)
                End If
            Else
                e.Effects = e.AllowedEffects And DragDropEffects.Move
                If ActionCopierEnCours Then
                    Try
                        If PlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me), e.Data,
                                                                            e.GetPosition(Me), e.Effects,
                                                                            "Déplacer vers %1", Header)
                    Catch ex As Exception
                    End Try
                    ActionCopierEnCours = False
                Else
                    If PlateformVista Then DropTargetHelper.DragOver(e.GetPosition(Me), e.Effects)
                End If
            End If
        ElseIf (e.Data.GetDataPresent("fileCDAudio")) Then
            If (e.KeyStates And DragDropKeyStates.ControlKey) = DragDropKeyStates.ControlKey Then
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me),
                                                                        e.Data, e.GetPosition(Me),
                                                                        e.Effects, "Copier vers %1", Header)
            Else
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(Me),
                                                                        e.Data, e.GetPosition(Me),
                                                                        e.Effects, "Convertir en vers sur %1", Header)
            End If
        Else
            e.Effects = DragDropEffects.None
        End If
        'Me.BringIntoView()
        e.Handled = True
    End Sub

    'Traitement du message de notification du timer d'expand des items lors d'un drag and drop and un delegate pour regler les problemes de thread
    Delegate Sub CallBackToTimerElapse(ByVal e As System.Timers.ElapsedEventArgs)
    Private Sub TimerExpandeItem_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles TimerExpandeItem.Elapsed
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, New CallBackToTimerElapse(AddressOf NotifyTimerElapsed), e)
    End Sub
    Private Sub NotifyTimerElapsed(ByVal e As System.Timers.ElapsedEventArgs)
        If TimerExpandeItem.Enabled Then
            TimerExpandeItem.Enabled = False
            If Me.IsExpanded Then
                Me.IsExpanded = False
                TimerExpandeItem.Interval = 800
                TimerExpandeItem.Enabled = True
            Else
                Me.IsExpanded = True
                TimerExpandeItem.Interval = 1000
                TimerExpandeItem.Enabled = True
            End If
        End If

    End Sub
End Class