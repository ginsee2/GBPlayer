Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Threading
Imports System.Resources
Imports System.Reflection
Imports System.ComponentModel
Imports System.Security.Permissions
Imports System.Security
Imports Microsoft.VisualBasic
Imports System.Management
Imports System.Windows.Controls.Primitives

Class MainWindow
    Private WithEvents DiscogsAuthentification As oAuthDiscogs = New oAuthDiscogs
    Private WithEvents ConvertisseurFichier As fileConverter
    Private BassInit As BassSystem
    Private WithEvents NavigateurWeb As WindowWebBrowser
    Private WithEvents LePlayer As WindowPlayer
    Private WithEvents Bibliotheque As tagID3Bibliotheque
    Private HistoriqueDesRecherches As List(Of String) = New List(Of String)
    Private WithEvents FenetreDiscogs As WindowsDiscogs
    Private WithEvents ScanDisque As wpfDrives
    Private MyUpdateManager As New UpdateManager
    Public Property ProcessMiseAJour As WindowUpdateProgress
    '    Private _DiskCollection As New ObservableCollection(Of String)
    '    Public ReadOnly Property DiskCollection As ObservableCollection(Of String)
    '        Get
    '            Return _DiskCollection ' If ScanDisque IsNot Nothing Then Return ScanDisque.DiskCollection Else Return Nothing '
    '        End Get
    '    End Property
    '  Public Property CDRomActif As BassCDDrive
    Private Delegate Sub NoArgDelegate()

    '***********************************************************************************************
    '---------------------------------INITIALISATION DE LA CLASSE-----------------------------------
    '***********************************************************************************************
    Dim RepertoireSelectionne As String

 
    Private Sub MainWindow_Initialized(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Initialized
        Dim HauteurEcran = SystemParameters.PrimaryScreenHeight
        Dim LargeurEcran = SystemParameters.PrimaryScreenWidth
        Me.Top = Application.Config.mainWindow_position.Y
        If Me.Top > HauteurEcran Or Me.Top < 0 Then Me.Top = 0
        Me.Left = Application.Config.mainWindow_position.X
        If Me.Left > LargeurEcran Or Me.Left < 0 Then Me.Left = 0
        Me.Height = Application.Config.mainWindow_size.Height
        If Me.Height > HauteurEcran - Me.Top Then Me.Height = HauteurEcran - Me.Top - 10
        Me.Width = Application.Config.mainWindow_size.Width
        If Me.Width > LargeurEcran - Me.Left Then Me.Width = LargeurEcran - Me.Left - 10
        ListeRepertoires.gbRacine = Application.Config.directoriesList_root
        ListeRepertoires.gbFolderSelected = Application.Config.directoriesList_activeDirectory
    End Sub
    Private Sub MainWindow_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Loaded
        ProcessMiseAJour = New WindowUpdateProgress(Me)
        Bibliotheque = New tagID3Bibliotheque()
        Bibliotheque.UpdateBibliotheque(ListeRepertoires.gbRacine, Me)
        If BassInit Is Nothing Then BassInit = New BassSystem(44100, dxMixer.DeviceSoundSystemList)
        ScanDisque = New wpfDrives(wpfDrives.EnumDriveUpdate.CDRomModification Or
                                       wpfDrives.EnumDriveUpdate.DiskAdd Or wpfDrives.EnumDriveUpdate.DiskDel)
        Dim ListeCDROM As List(Of String) = ScanDisque.GetCDRomDrives
        ListeFichiersMp3.DisplayValidation = True
        'ScanSystemDisk()

        Bibliotheque.CollectionList = ListeCollection
        Bibliotheque.WantList = ListeWantList
        MyUpdateManager.SubscribeCollectionPosts(ListeCollection)
        MyUpdateManager.SubscribeWantlistPosts(ListeWantList)
        MyUpdateManager.SubscribeCollectionPosts(Bibliotheque)
        MyUpdateManager.SubscribeWantlistPosts(Bibliotheque)
        Bibliotheque.SubscribeUpdateShellEvent(ListeFichiersMp3)
        Bibliotheque.SubscribeUpdateShellEvent(EditeurTAG2)
    End Sub
    '***********************************************************************************************
    '---------------------------------DESTRUCTION DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Private Sub MainWindow_Closing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        If ConvertisseurFichier IsNot Nothing Then
            If ConvertisseurFichier.IsAlive Then
                wpfMsgBox.MsgBoxInfo("APPLICATION CAN'T STOP", "Wait until the process of convertion before closing the application")
                e.Cancel = True
            End If
        End If
        If Bibliotheque IsNot Nothing Then
            If Bibliotheque.IsBusy Then
                If wpfMsgBox.MsgBoxQuestion("CRITICAL APPLICATION STOP", "The update of the library is in a critical phase," & _
                                            "want to wait before exiting the application?", , "Risk of loosing the library") Then
                    e.Cancel = True
                End If
            End If
        End If
    End Sub
    Private Sub MainWindow_Closed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Closed
        ForcageArretApplication()
    End Sub
    Public Sub ForcageArretApplication()
        Application.Config.mainWindow_position = New ConfigApp.ConfigPoint(Me.Left, Me.Top)
        Application.Config.mainWindow_size = New ConfigApp.ConfigSize(Me.Height, Me.Width)
        Application.Config.directoriesList_activeDirectory = ListeRepertoires.gbFolderSelected
        Application.Config.directoriesList_root = ListeRepertoires.gbRacine
        'oAuthDiscogs.SaveConfiguration() 'ConfigUtilisateur)
        ListeFichiersMp3.SaveConfiguration()
        ListeCollection.SaveConfiguration()
        ListeWantList.SaveConfiguration()
        ListeSellList.SaveConfiguration()
        ListePisteCD.SaveConfiguration()
        If LePlayer IsNot Nothing Then LePlayer.ClosePlayer()
        If NavigateurWeb IsNot Nothing Then NavigateurWeb.CloseWebBrowser()
        If ProcessMiseAJour IsNot Nothing Then ProcessMiseAJour.Close()
        If FenetreDiscogs IsNot Nothing Then FenetreDiscogs.Close()
        Bibliotheque.CloseBibliothèque()
        ScanDisque.Dispose()
    End Sub

    '**************************************************************************************************************
    '*****************GESTION DES DISQUES ET DE L'ACTIVITE DES LECTEURS DE CDROM***********************************
    '**************************************************************************************************************
    Private Sub ScanDisque_DiskAdded(ByVal Name As String) Handles ScanDisque.DiskAdded
        If Path.GetPathRoot(ListeRepertoires.gbRacine) = Path.GetPathRoot(Name) Then
            ListeRepertoires.gbRacine = ListeRepertoires.gbRacine
        End If
        Debug.Print("disque added :" & Name)
        'ScanSystemDisk()
    End Sub
    Private Sub ScanDisque_DiskCDRomEjected(ByVal Name As String) Handles ScanDisque.DiskCDRomEjected
        Debug.Print("CDRomActif ejected :" & Name)
        'ScanSystemDisk()
    End Sub
    Private Sub ScanDisque_DiskCDRomInsered(ByVal Name As String) Handles ScanDisque.DiskCDRomInsered
        Debug.Print("cdrom insered :" & Name)
        'ScanSystemDisk()
    End Sub
    Private Sub ScanDisque_DiskRemoved(ByVal Name As String) Handles ScanDisque.DiskRemoved
        If Path.GetPathRoot(ListeRepertoires.gbRacine) = Path.GetPathRoot(Name) Then
            ListeRepertoires.gbRacine = ListeRepertoires.gbRacine
        End If
        Debug.Print("disque removed :" & Name)
        'ScanSystemDisk()
    End Sub

    '**************************************************************************************************************
    '*****************GESTION DE LA MISE A JOUR DES REPERTOIRES****************************************************
    '**************************************************************************************************************
    Dim DirectoryNameUpdated As Boolean
    Private Sub ListeRepertoires_AfterUpdate(ByVal TypeOperation As String, ByVal NewPath As String) Handles ListeRepertoires.AfterUpdate
        DirectoryNameUpdated = True
    End Sub
    Sub ListeRepertoires_SelectionChanged(ByVal Path As String) Handles ListeRepertoires.SelectionChanged
        If Bibliotheque IsNot Nothing And Directory.Exists(Path) Then
            If (Bibliotheque.MiseAJourOk And ModeRecherche.IsChecked) Then
                Dim ChaineRecherche = "dir:" & RemplaceOccurences(Path, ",", "*")
                ListeFichiersMp3.MiseAJourListeFichierXElement(ChaineRecherche, , Path)
                Exit Sub
            End If
        End If
        ListeFichiersMp3.MiseAJourListeRepertoire(Path)
    End Sub
    Private Sub Bibliotheque_UpdateDirectoryName(ByVal NewName As String, ByVal oldName As String) Handles Bibliotheque.UpdateDirectoryName
        Me.Dispatcher.BeginInvoke(Sub()
                                      If Not DirectoryNameUpdated Then
                                          If (oldName = ListeRepertoires.gbFolderSelected) Or (NewName = ListeRepertoires.gbFolderSelected) Then
                                              ListeRepertoires_SelectionChanged(NewName)
                                          End If
                                      End If
                                      DirectoryNameUpdated = False
                                  End Sub)
    End Sub

    '**************************************************************************************************************
    '*****************PROCEDURES POUR LE TRAITEMENT DE LA POSITION DES FENETRES ENFANTS****************************
    '**************************************************************************************************************
    'PROCEDURES POUR LE TRAITEMENT DE LA TAILLE DE LA FENETRE
    'Procedure de réponse au message de modification de la taille de la fenetre
    Private Sub MainWindow_SizeChanged(ByVal sender As Object, ByVal e As System.Windows.SizeChangedEventArgs) Handles Me.SizeChanged
        For Each i As Window In Me.OwnedWindows
            Select Case i.Name
                Case "Player"
                    i.Left = Me.PointToScreen(Me.BordureLecteur.TranslatePoint(New Point(0, 0), Me)).X
                    i.Top = Me.PointToScreen(Me.BordureLecteur.TranslatePoint(New Point(0, 0), Me)).Y
                    i.Height = Me.BordureLecteur.ActualHeight
                    i.Width = Me.BordureLecteur.ActualWidth  '- Fenetre.PointToScreen(Fenetre.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Fenetre)).Y
                Case "Recorder"
                    i.Left = Me.PointToScreen(Me.BordureRecorder.TranslatePoint(New Point(0, 0), Me)).X
                    i.Top = Me.PointToScreen(Me.BordureRecorder.TranslatePoint(New Point(0, 0), Me)).Y
                    i.Height = Me.BordureRecorder.ActualHeight
                    i.Width = Me.BordureRecorder.ActualWidth  '- Fenetre.PointToScreen(Fenetre.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Fenetre)).Y
                Case "MiseAJour"
                    i.Left = Me.PointToScreen(Me.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Me)).X
                    i.Top = Me.PointToScreen(Me.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Me)).Y
                    If i.Visibility = Windows.Visibility.Visible Then
                        If (i.Left > Me.ActualWidth) Or ((i.Top) > Me.ActualHeight) Then
                            i.Visibility = Windows.Visibility.Collapsed
                        Else
                            i.Height = Me.BordureAffichageTemp.ActualHeight - 4
                            i.Width = Me.BordureAffichageTemp.ActualWidth - 5 '- Fenetre.PointToScreen(Fenetre.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Fenetre)).Y
                        End If
                    End If
            End Select
        Next i
    End Sub
    'Procedure de réponse au message de déplacement de la fenetre
    Public Sub DeplacementFenetre()
        For Each i As Window In Me.OwnedWindows
            Select Case i.Name
                Case "Player"
                    i.Left = Me.PointToScreen(Me.BordureLecteur.TranslatePoint(New Point(0, 0), Me)).X
                    i.Top = Me.PointToScreen(Me.BordureLecteur.TranslatePoint(New Point(0, 0), Me)).Y
                Case "Recorder"
                    i.Left = Me.PointToScreen(Me.BordureRecorder.TranslatePoint(New Point(0, 0), Me)).X
                    i.Top = Me.PointToScreen(Me.BordureRecorder.TranslatePoint(New Point(0, 0), Me)).Y
                Case "MiseAJour"
                    i.Left = Me.PointToScreen(Me.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Me)).X
                    i.Top = Me.PointToScreen(Me.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Me)).Y
            End Select
        Next i
    End Sub
    'Procedure de réponse au message de modification de la taille du cadre du lecteur
    Private Sub BordureLecteur_SizeChanged(ByVal sender As Object, ByVal e As System.Windows.SizeChangedEventArgs) Handles BordureLecteur.SizeChanged
        'For Each i As Window In Me.OwnedWindows
        'If (i.Name = "Player") Then
        ' i.Left = Me.PointToScreen(Me.BordureLecteur.TranslatePoint(New Point(0, 0), Me)).X
        ' i.Top = Me.PointToScreen(Me.BordureLecteur.TranslatePoint(New Point(0, 0), Me)).Y
        ' i.Height = e.NewSize.Height
        ' End If
        ' Next i
    End Sub
    'Procedure de réponse au message de modification de la position de la bordure d'affichage temp
    Private Sub SeparationVerticale_DragCompleted(ByVal sender As Object, ByVal e As System.Windows.Controls.Primitives.DragCompletedEventArgs) Handles SeparationVerticale.DragCompleted
        For Each i As Window In Me.OwnedWindows
            Select Case i.Name
                Case "MiseAJour"
                    i.Left = Me.PointToScreen(Me.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Me)).X
                    i.Top = Me.PointToScreen(Me.BordureAffichageTemp.TranslatePoint(New Point(0, 0), Me)).Y
            End Select
        Next i

    End Sub

    '**************************************************************************************************************
    '*****************************GESTION DE LA LISTE DES FICHIERS*************************************************
    '**************************************************************************************************************
    'PROCEDURE DE REPONSE LORS DU CHANGEMENT DE SELECTION
    Private Sub ListeFichiersMp3_SelectionChanged(ByVal ListeFichiersSel As System.Collections.Generic.List(Of String)) Handles ListeFichiersMp3.SelectionChanged
        If EditeurTAG2.IsLoaded Then EditeurTAG2.MiseAJourListeFichiers(ListeFichiersSel, Me)
    End Sub
    'PROCEDURE DE REPONSE POUR LES DOUBLES CLICS SUR LA LISTE
    Private Sub ListeFichiersMp3_SelectionDblClick(ByVal NomFichier As String, ByVal Tag As String) Handles ListeFichiersMp3.SelectionDblClick
        Try
            If LePlayer Is Nothing Then
                LePlayer = New WindowPlayer(Me, Bibliotheque)
                LePlayer.Show()
                If LePlayer.IsOk Then
                    LePlayer.PlayFichier(NomFichier)
                Else
                    LePlayer = Nothing
                End If
            Else
                LePlayer.PlayFichier(NomFichier)
            End If
        Catch ex As Exception
            wpfMsgBox.MsgBoxInfo("Erreur player", ex.Message)
        End Try

    End Sub
    'PROCEDURE DE DEMANDE DE CHANGEMENT DANS LA LISTE DES REPERTOIRES
    Private Sub ListeFichiersMp3_RequeteSelectionRepertoire(ByVal Repertoire As String) Handles ListeFichiersMp3.RequeteSelectionRepertoire
        ListeRepertoires.gbFolderSelected = Repertoire
    End Sub
    'PROCEDURE DE REPONSE AUX MESSAGES DE MISE A JOUR LISTE FICHIERS
    Private Sub ListeFichiersMp3_UpdateBegin() Handles ListeFichiersMp3.UpdateDebut
        VoyantUpdateListeFichiers.IsChecked = True
        NombreDeFichiers.Text = ""
    End Sub
    Private Sub ListeFichiersMp3_UpdateEnCours() Handles ListeFichiersMp3.UpdateEnCours
        NombreDeFichiers.Text = ListeFichiersMp3.FilesCollection.Count
    End Sub
    Private Sub ListeFichiersMp3_UpdateEnd() Handles ListeFichiersMp3.UpdateFin
        VoyantUpdateListeFichiers.IsChecked = False
    End Sub
    '**************************************************************************************************************
    '*****************************GESTION DE LA LISTE DES PISTES CD AUDIO******************************************
    '**************************************************************************************************************
    'PROCEDURE DE REPONSE POUR LES DOUBLES CLICS SUR LA LISTE
    Private Sub ListePisteCD_SelectionDblClick(ByVal NomFichier As String, ByVal Tag As String) Handles ListePisteCD.SelectionDblClick
        Try
            If LePlayer Is Nothing Then
                LePlayer = New WindowPlayer(Me, Bibliotheque)
                LePlayer.Show()
                If LePlayer.IsOk Then
                    LePlayer.PlayFichier(NomFichier)
                Else
                    LePlayer = Nothing
                End If
            Else
                LePlayer.PlayFichier(NomFichier)
            End If
        Catch ex As Exception
            wpfMsgBox.MsgBoxInfo("Player error", ex.Message)
        End Try

    End Sub

    '**************************************************************************************************************
    '*******************************************GESTION DU PLAYER**************************************************
    '**************************************************************************************************************
    Private Sub LePlayer_FichierSuivant(NumPiste As Integer) Handles LePlayer.FichierSuivant
        Dim Retour = ListeFichiersMp3.GetNext()
        If Retour <> "" Then
            LePlayer.PlayFichier(Retour, NumPiste)
        End If
    End Sub



    '**************************************************************************************************************
    '***********************************************GESTION DU NAVIGATEUR WEB**************************************
    '**************************************************************************************************************
    Private Sub NavigateurWeb_Closed(ByVal sender As Object, ByVal e As System.EventArgs) Handles NavigateurWeb.Closed
        MyUpdateManager.UnSubscribeCollectionPosts(NavigateurWeb)
        MyUpdateManager.UnSubscribeWantlistPosts(NavigateurWeb)
        NavigateurWeb = Nothing
    End Sub
    Private Sub NavigateurWeb_UpdateSource(ByVal NouvelleSource As String) Handles NavigateurWeb.UpdateSource, FenetreDiscogs.UpdateSource
        'Try
        If NouvelleSource <> "" Then
            Dim Onglet As TabItem = ListeOnglets.SelectedItem
            Select Case Onglet.Name
                Case "DétailsTAGID3v2"
                    EditeurTAG2.UpdateIdRelease(NouvelleSource)
                Case "GestionAchats"
                    ListeCollection.UpdateIdCollection(NouvelleSource)
                Case "GestionWantList"
                    ListeWantList.UpdateIdWantList(NouvelleSource)
                Case "GestionSellList"
                    ListeSellList.UpdateIdSellList(NouvelleSource)
            End Select
        End If
        'Catch ex As Exception
        'wpfMsgBox.MsgBoxInfo("Erreur de release", "La page sélectionnée ne contient pas de numéro de release valide", Me, NouvelleSource)
        ' End Try
    End Sub
    Private Sub NavigateurWeb_RequeteWebBrowser(ByVal url As System.Uri) Handles EditeurTAG2.RequeteWebBrowser,
                                                                                    ListeFichiersMp3.RequeteWebBrowser,
                                                                                    ListeCollection.RequeteWebBrowser,
                                                                                    ListeWantList.RequeteWebBrowser,
                                                                                    ListeSellList.RequeteWebBrowser
        If NavigateurWeb Is Nothing Then
            NavigateurWeb = New WindowWebBrowser()
            MyUpdateManager.SubscribeCollectionPosts(NavigateurWeb)
            MyUpdateManager.SubscribeWantlistPosts(NavigateurWeb)
        End If
        NavigateurWeb.Focus()
        NavigateurWeb.UpdateUrl(url)
    End Sub
    '**************************************************************************************************************
    '***********************************************GESTION OAUTH DISCOGS******************************************
    '**************************************************************************************************************
    Private Sub DiscogsAuthentification_ConsumerKeyRequest(ByVal SettingUri As Uri) Handles DiscogsAuthentification.ConsumerKeyRequest
        Me.Dispatcher.BeginInvoke(Sub()
                                      Dim navigateurconfig As WindowConfig = New WindowConfig()
                                      'Dim retour As String = wpfMsgBox.InputBox("Procédure d'autorisation accès Discogs?", Me, "Entrer le nom du compte Discogs", Application.Config.user_name)
                                      'If retour <> "" Then
                                      navigateurconfig.Focus()
                                      navigateurconfig.DemandeAcces()
                                      'End If
                                  End Sub, Windows.Threading.DispatcherPriority.Input, Nothing)

    End Sub
    Private Sub DiscogsAuthentification_StateChanged() Handles DiscogsAuthentification.StateChanged
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                 New NoArgDelegate(Sub()

                                                   End Sub))
    End Sub
    '**************************************************************************************************************
    '***********************************************GESTION DES ONGLETS**************************************
    '**************************************************************************************************************
    Private Sub ListeOnglets_SelectionChanged(ByVal sender As Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles ListeOnglets.SelectionChanged
        Dim SelectedItems As New List(Of String)
        If TypeOf (e.OriginalSource) Is TabControl Then
            Dim Onglet As TabItem = CType(e.OriginalSource, TabControl).SelectedItem
            For Each i In ListeFichiersMp3.ListeFichiers.SelectedItems
                SelectedItems.Add(CType(i, tagID3FilesInfosDO).NomComplet)
            Next
            Select Case Onglet.Name
                Case "DétailsTAGID3v2"
                    EditeurTAG2.MiseAJourListeFichiers(SelectedItems, Me)
                Case "GestionAchats"
                    ListeCollection.DisplayValidation = True
                Case "GestionCDAudio"
                    ListePisteCD.DisplayValidation = True
                Case "GestionWantList"
                    ListeWantList.DisplayValidation = True
                Case "GestionSellList"
                    ListeSellList.DisplayValidation = True
                Case "PhoneSynchro"
                    FenetreSynchro.DisplayValidation = True
                    ' ListeSellList.DisplayValidation = True
            End Select
        End If
    End Sub

    '**************************************************************************************************************
    '***********************************************GESTION DE LA BIBLIOTHEQUE*************************************
    '**************************************************************************************************************
    'Gestion de la mise à jour de la bibliotheque lors chargement chemin racine
    Private Sub BPChooseRacine_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles BPChooseRacine.Click
        Dim dialog = New wpfFolderBrowser ' System.Windows.Forms.FolderBrowserDialog
        dialog.SelectedPath = ListeRepertoires.gbRacine
        dialog.DialogTitle = "Select the root"
        dialog.ShowDialog()
        If dialog.SelectedPath <> "" Then
            ListeRepertoires.gbRacine = dialog.SelectedPath
            If Bibliotheque IsNot Nothing Then Bibliotheque.UpdateBibliotheque(ListeRepertoires.gbRacine, Me)
        End If
    End Sub
    Private Sub ModeRecherche_Checked(sender As Object, e As RoutedEventArgs) Handles ModeRecherche.Checked
        RechercheArtiste.IsEnabled = True
        If Bibliotheque.MiseAJourOk Then
            Dim ChaineRecherche As String = ""
            If (RechercheArtiste.IsEnabled) And (RechercheArtiste.Text <> "") Then
                Dim FichierSelectEnCours As String = ListeFichiersMp3.SelectedItem
                ChaineRecherche = RechercheArtiste.Text
                ListeFichiersMp3.MiseAJourListeFichierXElement(ChaineRecherche, FichierSelectEnCours)
            Else
                ChaineRecherche = "dir:" & RemplaceOccurences(ListeRepertoires.gbFolderSelected, ",", "*")
                ListeFichiersMp3.MiseAJourListeFichierXElement(ChaineRecherche, , ListeRepertoires.gbFolderSelected)
            End If
            Exit Sub
        End If
    End Sub
    Private Sub ModeRecherche_Unchecked(sender As Object, e As RoutedEventArgs) Handles ModeRecherche.Unchecked
        HistoriqueDesRecherches.Clear()
        RechercheArtiste.Text = ""
        MemRecherchePrecedente = ""
        RechercheArtiste.IsEnabled = False
        RecherchePrecedente.IsEnabled = False
        ListeFichiersMp3.MiseAJourListeRepertoire(ListeRepertoires.gbFolderSelected)
    End Sub

    'Gestion de la mise à jour de la bibliotheque
    Private Sub Bibliotheque_AfterUpdate(ByVal MiseAJourOk As Boolean) Handles Bibliotheque.AfterUpdate
        If MiseAJourOk Then
            Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                     New NoArgDelegate(Sub()
                                                           ModeRecherche.IsChecked = True
                                                       End Sub))
        Else
            Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                     New NoArgDelegate(Sub()
                                                           ModeRecherche.IsChecked = False
                                                       End Sub))
        End If
    End Sub
    Private Sub Bibliotheque_BeforeUpdate() Handles Bibliotheque.BeforeUpdate
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                 New NoArgDelegate(Sub()
                                                       RechercheArtiste.IsEnabled = False
                                                   End Sub))
    End Sub

    'Traitement des boutons de recherche
    Dim MemRecherchePrecedente As String = ""
    Private Sub RechercheVinyl_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles RechercheVinyl.Click
        '    RequeteRecherche(RechercheArtiste.Text, True)
    End Sub
    Private Sub RechercheArtiste_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Input.KeyEventArgs) Handles RechercheArtiste.KeyDown
        If e.Key = Key.Enter Then
            RequeteRecherche(RechercheArtiste.Text, True)
        End If
    End Sub
    Private Sub RecherchePrecedente_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles RecherchePrecedente.Click
        Dim ItemSelected As String = ""
        If HistoriqueDesRecherches.Count > 0 Then
            If HistoriqueDesRecherches.Count > 0 Then
                RechercheArtiste.Text = ExtraitChaine(HistoriqueDesRecherches.Last, "Recherche:", "\\{", 10)
                ItemSelected = ExtraitChaine(HistoriqueDesRecherches.Last, "\\{", "", 3)
                ListeFichiersMp3.MiseAJourListeFichierXElement(RechercheArtiste.Text, ItemSelected)
            End If
            If RechercheArtiste.Text = "" Then
                RecherchePrecedente.IsEnabled = False
                If Bibliotheque.MiseAJourOk Then
                    Dim ChaineRecherche As String = "dir:" & ListeRepertoires.gbFolderSelected
                    ListeFichiersMp3.MiseAJourListeFichierXElement(ChaineRecherche, ItemSelected, ListeRepertoires.gbFolderSelected)
                Else
                    ListeFichiersMp3.MiseAJourListeRepertoire(ListeRepertoires.gbFolderSelected, ItemSelected)
                End If
            End If
            MemRecherchePrecedente = RechercheArtiste.Text
            HistoriqueDesRecherches.RemoveRange(HistoriqueDesRecherches.Count - 1, 1)
        End If
    End Sub
    Private Sub AnnuleRechercheEnCours_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles AnnuleRechercheEnCours.Click
        'If Not AnnuleRechercheEnCours.IsChecked Then
        HistoriqueDesRecherches.Clear()
        RechercheArtiste.Text = ""
        If Bibliotheque.MiseAJourOk Then
            Dim ChaineRecherche As String = "dir:" & ListeRepertoires.gbFolderSelected
            ListeFichiersMp3.MiseAJourListeFichierXElement(ChaineRecherche, , ListeRepertoires.gbFolderSelected)
        Else
            ListeFichiersMp3.MiseAJourListeRepertoire(ListeRepertoires.gbFolderSelected)
        End If
        RecherchePrecedente.IsEnabled = False
        MemRecherchePrecedente = ""
        'End If
    End Sub

    'Repond aux requetes de recherche venant des objets appartenant à la fenetre principale
    Private Sub RequeteRechercheFichiers(ByVal ChaineRecherche As String) Handles EditeurTAG2.RequeteRecherche,
                                                                                    LePlayer.RequeteRecherche,
                                                                                    NavigateurWeb.RequeteRecherche
        RequeteRecherche(ChaineRecherche, True)
    End Sub
    Private Sub RequeteRechercheListeFichiers(ByVal ChaineRecherche As String, ByVal NewRequete As Boolean) Handles ListeFichiersMp3.RequeteRecherche,
                                                                                                                    ListeCollection.RequeteRecherche,
                                                                                                                    ListeWantList.RequeteRecherche,
                                                                                                                    ListeSellList.RequeteRecherche
        RequeteRecherche(ChaineRecherche, NewRequete)
    End Sub
    Private Sub RequeteRecherche(ByVal ChaineRecherche As String, ByVal NewRequete As Boolean)
        If Bibliotheque.MiseAJourOk And ModeRecherche.IsChecked Then
            If (NewRequete) Then ' Or (Microsoft.VisualBasic.Left(RechercheArtiste.Text, 1) <> "?") Then
                If ChaineRecherche = "" Then
                    RechercheArtiste.Text = ""
                    RecherchePrecedente.IsEnabled = False
                    If Bibliotheque.MiseAJourOk Then
                        ChaineRecherche = "dir:" & ListeRepertoires.gbFolderSelected
                        ListeFichiersMp3.MiseAJourListeFichierXElement(ChaineRecherche, , ListeRepertoires.gbFolderSelected)
                    Else
                        ListeFichiersMp3.MiseAJourListeRepertoire(ListeRepertoires.gbFolderSelected)
                    End If
                    Exit Sub
                End If
                RechercheArtiste.Text = ChaineRecherche
            Else
                RechercheArtiste.Text = RechercheArtiste.Text & "," & ChaineRecherche
            End If
            If RechercheFichier.IsChecked Then
                ChaineRecherche = RechercheArtiste.Text
                ListeFichiersMp3.MiseAJourListeFichierXElement(ChaineRecherche)
                If RechercheArtiste.Text <> "" Then
                    '                HistoriqueDesRecherches.Add("Recherche:" & RechercheArtiste.Text & "\\[" & ListeFichiersMp3.SelectedItem & "]")
                    HistoriqueDesRecherches.Add("Recherche:" & MemRecherchePrecedente & "\\{" & ListeFichiersMp3.SelectedItem)
                    RecherchePrecedente.IsEnabled = True
                End If
            End If
            MemRecherchePrecedente = RechercheArtiste.Text
            ListeCollection.FiltreVinyls(RechercheArtiste.Text, True)
            ListeWantList.FiltreVinyls(RechercheArtiste.Text, True)
            ListeSellList.FiltreVinyls(RechercheArtiste.Text, True)
        End If
    End Sub

    'Repond aux requetes du menu conditionnel de la saisie recherche
    Private Sub RechercheArtiste_PreviewMouseRightButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles RechercheArtiste.PreviewMouseRightButtonDown
        RechercheArtiste.ContextMenu = Bibliotheque.CreationMenuContextuelDynamique("Recherche", New RoutedEventHandler(AddressOf MenuDynamique_Click))
    End Sub
    Private Sub MenuDynamique_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
        If CType(e.OriginalSource, MenuItem).Name Like "Groupement*" Then
            RechercheArtiste.Text = "subgroup:" & CType(e.OriginalSource, MenuItem).Header
        ElseIf CType(e.OriginalSource, MenuItem).Name Like "Style*" Then
            RechercheArtiste.Text = "style:" & CType(e.OriginalSource, MenuItem).Header
        End If
        RequeteRecherche(RechercheArtiste.Text, True)
    End Sub
    'Requete d'une fenetre pour affichage menu contextuel créé par la bibliotheque
    Private Sub EditeurTAG2_RequeteMenuContextuel(ByVal NomChamp As String, ByRef MenuContextuelGroupement As System.Windows.Controls.ContextMenu, ByVal FonctionReponse As RoutedEventHandler) Handles EditeurTAG2.RequeteMenuContextuel
        MenuContextuelGroupement = Bibliotheque.CreationMenuContextuelDynamique(NomChamp, FonctionReponse)
    End Sub

    '**************************************************************************************************************
    '***********************************************GESTION DE LA COLLECTION*************************************
    '**************************************************************************************************************
    'Repond à la demande de la collection en renvoyant le fichier selectionne en cours
    Private Sub ListeCollection_UpdateLink(ByRef FichierALier As String) Handles ListeCollection.UpdateLink
        FichierALier = ListeFichiersMp3.SelectedItem
    End Sub

    '**************************************************************************************************************
    '***********************************************GESTION DES INFOS DISCOGS**************************************
    '**************************************************************************************************************
    'Repond à la demande de mise à jour d'affichage de la fenetre d'infos Discogs
    Private Sub FenetreDiscogs_RequeteInfosDiscogs(ByVal id As String, ByVal typeinfo As NavigationDiscogsType) Handles EditeurTAG2.RequeteInfosDiscogs,ListeCollection.RequeteInfosDiscogs
        If FenetreDiscogs Is Nothing Then
            FenetreDiscogs = New WindowsDiscogs
        End If
        Try
            Select typeinfo
                Case NavigationDiscogsType.release
                    FenetreDiscogs.AfficheRelease(id)
                Case NavigationDiscogsType.updaterelease
                    If FenetreDiscogs.Visibility = Windows.Visibility.Visible Then FenetreDiscogs.AfficheRelease(id)
            End Select
        Catch ex As Exception
            Debug.Print("ok")
        End Try
    End Sub

    '**************************************************************************************************************
    '***********************************************GESTION DES CONVERSIONS****************************************
    '**************************************************************************************************************
    Private NumProcess As Long
    Private Delegate Sub UpdateWindowsDelegate(ByVal NomFichier As String, ByVal Avancement As Integer)
    Private Sub UpdateWindows(ByVal NomFichier As String, ByVal Avancement As Integer)
        If InStr(NomFichier, "#INIT#") Then
            NumProcess = Me.ProcessMiseAJour.AddNewProcess(CInt(ExtraitChaine(NomFichier, "#INIT#", "", 6)), NumProcess)
        ElseIf NomFichier = "#END#" Then
            Me.ProcessMiseAJour.StopProcess(NumProcess)
            NumProcess = 0
        ElseIf Avancement > 0 Then
            Me.ProcessMiseAJour.UpdateWindows("Conversion in progress...: " & NomFichier, NumProcess, True, Avancement)
        Else
            Me.ProcessMiseAJour.UpdateWindows(NomFichier, NumProcess)
        End If
    End Sub
    Private NbrConversion As Integer
    Private Delegate Sub TacheConvertion(ByVal NomFichier As String, ByVal Repertoire As String, ByVal TypeConversion As fileConverter.ConvertType)
    Private Sub Converter_RequeteConvertFile(ByVal TabFichiers() As String, ByVal Destination As String,
                                             ByVal typeConversion As fileConverter.ConvertType) Handles ListeRepertoires.RequeteConvertFile,
                                                                                                        ListeFichiersMp3.RequeteConvertFile
        Try
            UpdateWindows("#INIT#" & TabFichiers.Count, 0)
            For Each i In TabFichiers
                Dim ReadTag As New TacheConvertion(AddressOf TacheConvertionEnCours)
                ReadTag.BeginInvoke(i, Destination, typeConversion, Nothing, Nothing)
                Thread.Sleep(200)
            Next
            NbrConversion += 1
        Catch ex As Exception
        End Try
    End Sub
    Sub TacheConvertionEnCours(ByVal NomFichier As String, ByVal Repertoire As String, ByVal TypeConversion As fileConverter.ConvertType)
        If ConvertisseurFichier Is Nothing Then
            ConvertisseurFichier = New fileConverter
            AddHandler ConvertisseurFichier.ProcessProgress, AddressOf Converter_ProcessProgress
            AddHandler ConvertisseurFichier.ProcessStart, AddressOf Converter_ProcessStart
            AddHandler ConvertisseurFichier.ProcessStop, AddressOf Converter_ProcessStop
        End If
        If TypeConversion = fileConverter.ConvertType.mp3 Then
            If Not ConvertisseurFichier.ConvertToMp3(NomFichier, Repertoire, "", fileEncoderMp3.EnumBitrateEncodeur.Bitrate_320) Then
                wpfMsgBox.MsgBoxInfoThread("Mp3 conversion error", "An error occurred during the operation of file conversion.", , Path.GetFileName(NomFichier))
            End If
        Else
            If Not ConvertisseurFichier.ConvertToWave(NomFichier, Repertoire) Then
                wpfMsgBox.MsgBoxInfoThread("Mp3 conversion error", "An error occurred during the operation of file conversion.", , Path.GetFileName(NomFichier))
            End If
        End If

    End Sub
    Public Sub Converter_ProcessProgress(ByVal fichierEnCours As String, ByVal value As Integer)
        Fenetre.Dispatcher.BeginInvoke(New UpdateWindowsDelegate(AddressOf UpdateWindows),
                                       System.Windows.Threading.DispatcherPriority.Background,
                                        {Path.GetFileName(fichierEnCours), value})
    End Sub
    Public Sub Converter_ProcessStart(ByVal fichierEnCours As String)
        Fenetre.Dispatcher.BeginInvoke(New UpdateWindowsDelegate(AddressOf UpdateWindows),
                                       System.Windows.Threading.DispatcherPriority.Background,
                                        {Path.GetFileName(fichierEnCours), 0})
    End Sub
    Public Sub Converter_ProcessStop(ByVal fichierEnCours As String)
        NbrConversion -= 1
        If NbrConversion = 0 Then
            Fenetre.Dispatcher.BeginInvoke(New UpdateWindowsDelegate(AddressOf UpdateWindows),
                                           System.Windows.Threading.DispatcherPriority.Background,
                                           {"#END#", 0})
        End If
    End Sub




    Private Infos As FenetreInfo
    Dim FenetreCD As Window1
    Private Sub EssaiBASS_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) 'Handles EssaiBASS.Click
        If Infos Is Nothing Then
            Dim TopFenetreParente As Integer
            Dim LeftFenetreParente As Integer
            Dim FenetreParente As MainWindow = CType(Application.Current.MainWindow, MainWindow)
            TopFenetreParente = FenetreParente.PointToScreen(FenetreParente.BordureAffichageTemp.TranslatePoint(New Point(0, 0), FenetreParente)).Y
            LeftFenetreParente = FenetreParente.PointToScreen(FenetreParente.BordureAffichageTemp.TranslatePoint(New Point(0, 0), FenetreParente)).X
            Infos = New FenetreInfo()
            Infos.Owner = FenetreParente
            Infos.WindowStartupLocation = WindowStartupLocation.Manual
            Infos.Top = TopFenetreParente - 60
            Infos.Left = LeftFenetreParente
            Infos.Height = 60
            Infos.addInfos("essai1")
            Infos.addInfos("essai2")
            Infos.Show()
        Else
            Infos.Close()
            Infos = Nothing
        End If
        'FenetreInfo.NumEnCours.Text = compteur
        'FenetreInfo.NbrTotal.Text = Maxi.ToString
        'FenetreInfo.ProgressionTraitement.Maximum = CInt(FenetreInfo.NbrTotal.Text.ToString)
        'FenetreInfo.ProgressionTraitement.Value = compteur
        'FenetreInfo.NomFichierEnCoursTraitement.Text = NomFichier
        'FenetreInfo.NomPropriétéEnCoursTraitement.Text = NomPropriete    End Sub
    End Sub

    Dim balloon As WindowPlayerMinimized
    Private Sub MainWindowNotifyIcon_IsVisibleChanged(ByVal sender As Object, ByVal e As System.Windows.DependencyPropertyChangedEventArgs) Handles MainWindowNotifyIcon.IsVisibleChanged
        If CType(e.NewValue, Boolean) = True Then
            If LePlayer IsNot Nothing Then
                balloon = New WindowPlayerMinimized(LePlayer.GetPrincipalTrack())
            Else
                balloon = New WindowPlayerMinimized(Nothing)
            End If
            MainWindowNotifyIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, Nothing)
        Else
            If LePlayer IsNot Nothing Then LePlayer.RestorePrincipalTrack(balloon.GetPrincipalTrack())
            MainWindowNotifyIcon.CloseBalloon()
            balloon = Nothing
        End If
    End Sub

    Private Sub TestPurge_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        '' For Each i In Directory.EnumerateDirectories("Ordinateur\GT-I8190\Phone")
        ' MsgBox(i)
        ' Next
        'Exit Sub
        'Bibliotheque.PurgerImagesInutiles()
        Dim guid As String = GetProfileGUID()
        Dim userID As String = Application.Config.user_id
        Dim DicRetour = FreeServer.TestValidUser("", guid, userID)
        ' MsgBox(DicRetour)
        If DicRetour.ContainsKey("UserID") Then
            'Application.Config.discogsConnection_tokenValue = AccessTokenData.TokenValue
            'Application.Config.discogsConnection_tokenSecret = AccessTokenData.TokenSecret
            'Application.Config.user_name = UserName
            Application.Config.user_id = DicRetour.Item("UserID")
            Return
        Else
            userID = FreeServer.Inscription("", guid)
            'MsgBox(userID)
            DicRetour = FreeServer.TestValidUser("", guid, userID)
            'MsgBox(DicRetour)
            If DicRetour.ContainsKey("UserID") Then
                'Application.Config.discogsConnection_tokenValue = AccessTokenData.TokenValue
                'Application.Config.discogsConnection_tokenSecret = AccessTokenData.TokenSecret
                'Application.Config.user_name = UserName
                Application.Config.user_id = DicRetour.Item("UserID")
                Return
            End If
        End If
    End Sub

End Class

Public Class FValidationRepertoire
    Inherits ValidationRule
    Public Overrides Function Validate(ByVal value As Object, ByVal cultureInfo As System.Globalization.CultureInfo) As System.Windows.Controls.ValidationResult
        If Directory.Exists(CType(value, String)) Then
            Return New ValidationResult(True, Nothing)
        Else
            Return New ValidationResult(False, "Le répertoire saisi n'existe pas!!")
        End If
    End Function
End Class

