'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 20/05/12
'DESCRIPTION :Classe de piste lecteur audio
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Option Compare Text

Imports System.Windows.Controls.Primitives
Imports System.IO
Imports System.Windows.Media.Animation
Imports System.Threading
Imports System

'***********************************************************************************************
'---------------------------------TYPES PUBLIC DE LA CLASSE------------------------------------
'***********************************************************************************************
Public Class PistePlayer
    Implements iNotifyShellUpdate
    Private Const TempsMixage = 2
    Private Const PenteMixage = 3
    Private Const PointMixage = 0.66

    '***********************************************************************************************
    '---------------------------------CONSTANTES PRIVEES DE LA CLASSE-------------------------------
    '***********************************************************************************************
    Private Const GBAU_NOMDOSSIER_TEMP = "GBDev\GBPlayer\Temp\"
    Private Enum EtatPlayer As Integer
        Arret = 0
        Play = 1
        Pause = 2
    End Enum
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES EVENEMENTS DU CONTROLE
    Public Event RequeteRecherche As EventHandler(Of RequeteRechercheEventArgs)
    Public Event RequeteFichierSuivant As EventHandler

    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES VARIABLES
    'Private WithEvents Mixer As dxMixer
    Private MixerLine As dxMixerLine
    Private MixerLine2 As dxMixerLine
    Private WithEvents LineEnCours As dxMixerLine
    Private LinePrecedente As dxMixerLine

    Private ParentPlayer As WindowPlayer
    Private WithEvents RafraicheAffichage As Timers.Timer = New Timers.Timer
    Private WithEvents Mixage As Timers.Timer = New Timers.Timer
    Private WithEvents ShellWatcher As New System.IO.FileSystemWatcher
    Private Etat As EtatPlayer = EtatPlayer.Arret
    Private RepDest As String
    Private NomFichierEnCours As String
    Private FichierTempEnCours As String
    Private FichierTempAEffacer As String
    Private InfosFichier As tagID3FilesInfosDO
    Private BlockageMaJPosition As Boolean
    Private WaveOctetParSec As Integer
    Private IdPiste2 As String

    Public Property IdPiste As String
    Public Property BibliothequeLiee As tagID3Bibliotheque Implements iNotifyShellUpdate.BibliothequeLiee
    Public Property Filter As String Implements iNotifyShellUpdate.Filter
    Private DemandeFichierSuivantEnCours As Boolean
    Public Property Volume As Integer
        Get
            Return PlayerVolume.Value
        End Get
        Set(ByVal value As Integer)
            PlayerVolume.Value = value
        End Set
    End Property
    Public ReadOnly Property IsPlayed As Boolean
        Get
            If Etat = EtatPlayer.Play Then Return True Else Return False
        End Get
    End Property
    Public ReadOnly Property FilePlayedName As String
        Get
            Return NomFichierEnCours
        End Get
    End Property
    Public ReadOnly Property PlayPosition As Integer
        Get
            Return PlayerPosition.Value
        End Get
    End Property

    '***********************************************************************************************
    '---------------------------------INITIALISATION DE LA CLASSE-----------------------------------
    '***********************************************************************************************
    Sub New()
        InitializeComponent()
    End Sub
    Sub New(ByVal NumPiste As String, ByVal Player As WindowPlayer) ', ByVal TableMixage As dxMixer)
        ' Cet appel est requis par le concepteur.
        InitializeComponent()
        Me.AddHandler(EqualSlider.ValueChangedEvent, New RoutedEventHandler(AddressOf EqualChangeValue))
        RepDest = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) & "\" & GBAU_NOMDOSSIER_TEMP
        'Mixer = TableMixage
        ParentPlayer = Player
        IdPiste = NumPiste
        IdPiste2 = IdPiste & "-b"
        RafraicheAffichage.Interval = 50
        Mixage.Interval = (TempsMixage * 1000) * (PenteMixage / 100)
        PlayerPosition.IsEnabled = False
        ParentPlayer.Mixer.AddLine(IdPiste)
        MixerLine = ParentPlayer.Mixer.LineByName(IdPiste)
        ParentPlayer.Mixer.AddLine(IdPiste2)
        MixerLine2 = ParentPlayer.Mixer.LineByName(IdPiste2)
        MixerLine2.Volume = 0
    End Sub

    '***********************************************************************************************
    '---------------------------------FONCTIONS PUBLIQUES DE LA CLASSE------------------------------
    '***********************************************************************************************
    Dim IDTacheEnCours As String
    Dim IDFichierEnCours As String
    Dim IDLineEnCours As String
    Dim FichierDiscAudio As Boolean
    Public Sub CloseTrack()
        ShellWatcher.EnableRaisingEvents = False
        If ParentPlayer.Mixer IsNot Nothing Then ParentPlayer.Mixer.RemoveLine(IdPiste)
        If ParentPlayer.Mixer IsNot Nothing Then ParentPlayer.Mixer.RemoveLine(IdPiste2)
    End Sub
    Public Function PlayFichier(ByVal NomFichier As String, Optional ByVal StopTrack As Boolean = False, Optional ByVal Position As Integer = 0) As Boolean
        Try
            Dim Reprise As Boolean = (Etat = EtatPlayer.Pause)
            Dim PremiereLecture As Boolean = (LinePrecedente Is Nothing) Or (Etat = EtatPlayer.Arret)
            If NomFichier <> IDFichierEnCours Or Etat = EtatPlayer.Arret Or Etat = EtatPlayer.Pause Then
                FichierDiscAudio = Path.GetExtension(NomFichier) = ".cd"
                ' If Mixer.IsOn Then
                Dim FichierTemp As String = NomFichier
                If Not FichierDiscAudio Then
                    FichierTemp = RepDest & Path.GetFileName(NomFichier)
                    If Not File.Exists(FichierTemp) Then
                        FileIO.FileSystem.CopyFile(NomFichier, FichierTemp)
                        Dim info As FileInfo = New FileInfo(FichierTemp)
                        info.IsReadOnly = False
                    End If
                    NomFichierEnCours = NomFichier
                    FichierTempEnCours = FichierTemp
                Else
                    NomFichierEnCours = NomFichier
                    FichierTempEnCours = NomFichier
                    EnchainementMorceaux.IsChecked = False
                End If
                If Not Reprise Then LineEnCours = GetNextLine()
                If LineEnCours.OpenLine(FichierTempEnCours) Then 'CHANGEMENT
                    IDFichierEnCours = NomFichier
                    IDTacheEnCours = FichierTemp
                    If FichierTempAEffacer <> "" Then
                        Try
                            If File.Exists(FichierTempAEffacer) And FichierTempAEffacer <> FichierTempEnCours Then
                                FileIO.FileSystem.DeleteFile(FichierTempAEffacer)
                            End If
                        Catch ex As Exception
                        End Try
                    End If
                    UpdateFilesInfos()
                    If Not StopTrack Then
                        If Not FichierDiscAudio Then FichierTempAEffacer = FichierTempEnCours
                        BpmGo.Content = "Bpm"
                        If (Not FichierDiscAudio) Then
                            BpmGo.IsEnabled = True
                        Else
                            BpmGo.IsEnabled = False
                        End If
                        Call LineEnCours.Play()
                        Mixage.Start()
                        DemandeFichierSuivantEnCours = False
                        ' LineEnCours.Position = Position
                        If PremiereLecture Or Reprise Then
                            LineEnCours.Volume = PlayerVolume.Value
                            '    LineEnCours.VolumeCasque = CasqueVolume.Value
                        Else
                            LineEnCours.Volume = 0
                            '    LineEnCours.VolumeCasque = CasqueVolume.Value
                        End If
                        imBpPlay.Source = GetBitmapImage("../Images/imgboutons/playtravail24.png")
                        imBpStop.Source = GetBitmapImage("../Images/imgboutons/stop24.png")
                        imBpPause.Source = GetBitmapImage("../Images/imgboutons/pause24.png")
                        Etat = EtatPlayer.Play
                        'UpdateFilesInfos()
                        DemarrageAnimationVinyl()
                        VinylImage.Visibility = Windows.Visibility.Visible
                        PlayerPosition.IsEnabled = True
                    Else
                        VinylImage.Visibility = Windows.Visibility.Visible
                    End If
                End If
                'End If
            End If
        Catch ex As Exception
            MsgBox(ex.Message & Chr(13) & ex.StackTrace)
            ' PlayFichierwpf(NomFichier)
        End Try
        Return True
    End Function

    Private Sub UpdateFilesInfos(Optional ByVal infos As tagID3FilesInfos = Nothing)
        If infos Is Nothing Then infos = New tagID3FilesInfos(NomFichierEnCours)
        InfosFichier = CType(FindResource("InfosFichier"), tagID3FilesInfosDO)
        InfosFichier.Update(infos)
        If File.Exists(NomFichierEnCours) Then
            ShellWatcher.IncludeSubdirectories = True ' False
            ShellWatcher.Path = CType(Application.Current.MainWindow, MainWindow).ListeRepertoires.gbRacine
            ShellWatcher.Filter = "*.mp3"
            ShellWatcher.NotifyFilter = NotifyFilters.LastWrite Or NotifyFilters.FileName
            ShellWatcher.EnableRaisingEvents = True
        End If
        InfosFichier.Selected = True
    End Sub
    Private Function GetNextLine() As dxMixerLine
        If LineEnCours IsNot Nothing Then
            LinePrecedente = LineEnCours
            '         Mixage.Start()
        End If
        If IDLineEnCours = "" Then
            IDLineEnCours = IdPiste
            LineEnCours = MixerLine
            Return MixerLine
        ElseIf IDLineEnCours = IdPiste Then
            IDLineEnCours = IdPiste2
            LineEnCours = MixerLine2
            Return MixerLine2
        Else
            IDLineEnCours = IdPiste
            LineEnCours = MixerLine
            Return MixerLine
        End If
    End Function
    '***********************************************************************************************
    '---------------------------------GESTION INTERFACE---------------------------------------------
    '***********************************************************************************************
    Private Sub BPPlay_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BPPlay.Click
        PlayFichier(NomFichierEnCours)
    End Sub
    Private Sub BPPlay_PreviewMouseDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles BPPlay.PreviewMouseDown
        If Etat <> EtatPlayer.Play Then
            imBpPlay.Source = GetBitmapImage("../Images/imgboutons/playtravail24.png")
            BPPlay.CaptureMouse()
        End If
    End Sub
    Private Sub BPPlay_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles BPPlay.MouseLeave
        If Etat <> EtatPlayer.Play Then imBpPlay.Source = GetBitmapImage("../Images/imgboutons/play24.png")
    End Sub

    Private Sub BPPause_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BPPause.Click
        If LineEnCours.IsOn Then
            LineEnCours.PausePlay()
            imBpPlay.Source = GetBitmapImage("../Images/imgboutons/play24.png")
            Etat = EtatPlayer.Pause
            ArretAnimationVinyl()
            Droite.Value = 0
            Gauche.Value = 0
        End If
    End Sub
    Private Sub BPPause_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles BPPause.MouseLeave
        If Etat <> EtatPlayer.Pause Then imBpPause.Source = GetBitmapImage("../Images/imgboutons/pause24.png")
    End Sub
    Private Sub BPPause_PreviewMouseDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles BPPause.PreviewMouseDown
        If Etat = EtatPlayer.Play Then
            imBpPause.Source = GetBitmapImage("../Images/imgboutons/pausetravail24.png")
        End If
    End Sub

    Private Sub BPStop_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BPStop.Click
        StopPlay()
    End Sub
    Public Sub StopPlay()
        If LineEnCours.IsOn Then
            LineEnCours.CloseLine()
            imBpPlay.Source = GetBitmapImage("../Images/imgboutons/play24.png")
            imBpStop.Source = GetBitmapImage("../Images/imgboutons/stop24.png")
            Etat = EtatPlayer.Arret
            ArretAnimationVinyl()
            RafraicheAffichage.Enabled = False
            PlayerPosition.IsEnabled = False
            PlayerPosition.Value = 0
            Droite.Value = 0
            Gauche.Value = 0
            Temps.Text = "00 : 00"
        End If
        imBpPause.Source = GetBitmapImage("../Images/imgboutons/pause24.png")
    End Sub

    Private Sub BPStop_PreviewMouseDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles BPStop.PreviewMouseDown
        If Etat = EtatPlayer.Play Or Etat = EtatPlayer.Pause Then
            imBpStop.Source = GetBitmapImage("../Images/imgboutons/stoptravail24.png")
        End If
    End Sub
    Private Sub BPStop_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles BPStop.MouseLeave
        If Etat <> EtatPlayer.Arret Then imBpStop.Source = GetBitmapImage("../Images/imgboutons/stop24.png")
    End Sub

    Private Sub BpmGo_Click(sender As Object, e As RoutedEventArgs) Handles BpmGo.Click
        If ((Not FichierDiscAudio) And (LineEnCours IsNot Nothing)) Then
            LineEnCours.StartBpmCalculate()
            BpmGo.IsEnabled = False
        End If
    End Sub

    'TRAITEMENT DE LA BARRE DE VOLUME
    Private Sub PlayerVolume_MouseWheel(ByVal sender As Object, ByVal e As System.Windows.Input.MouseWheelEventArgs) Handles VinylImage.MouseWheel, PlayerVolume.MouseWheel, tagImage.MouseWheel
        If (PlayerVolume.Value + (e.Delta / 20)) > PlayerVolume.Maximum Then
            PlayerVolume.Value = PlayerVolume.Maximum
        ElseIf (PlayerVolume.Value + (e.Delta / 20)) < PlayerVolume.Minimum Then
            PlayerVolume.Value = PlayerVolume.Minimum
        Else
            PlayerVolume.Value = PlayerVolume.Value + (e.Delta / 20)
        End If
    End Sub
    Private Sub PlayerVolume_ValueChanged(ByVal sender As Object, ByVal e As System.Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles PlayerVolume.ValueChanged
        If LineEnCours IsNot Nothing Then
            If LineEnCours.IsOn Then
                LineEnCours.Volume = e.NewValue
            End If
        End If
    End Sub

    'TRAITEMENT DE LA BARRE D'AVANCEMENT
    Private Sub PlayerPosition_PreviewMouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles PlayerPosition.PreviewMouseLeftButtonDown
        BlockageMaJPosition = True
    End Sub
    Private Sub PlayerPosition_PreviewMouseLeftButtonUp(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles PlayerPosition.PreviewMouseLeftButtonUp
        If LineEnCours IsNot Nothing Then
            If TypeOf (e.OriginalSource) Is Thumb Then
                LineEnCours.Position = PlayerPosition.Value
            Else
                LineEnCours.Position = ((PlayerPosition.Maximum - PlayerPosition.Minimum) / PlayerPosition.ActualWidth * e.GetPosition(PlayerPosition).X)
            End If
            BlockageMaJPosition = False
        End If
    End Sub
    Private Sub PlayerPosition_ValueChanged(ByVal sender As Object, ByVal e As System.Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles PlayerPosition.ValueChanged
        If BlockageMaJPosition Then
            Dim Minutes As Integer = Int(e.NewValue / 60)
            Dim Secondes As Integer = Int((e.NewValue) - Minutes * 60)
            Temps.Text = Minutes.ToString("00") & " : " & Secondes.ToString("00")
        End If
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DES MENUS CONTEXTUELS---------------------------------
    '***********************************************************************************************
    Private Async Sub PistePlayer_PreviewMouseRightButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseRightButtonDown
        If tagArtiste.IsAncestorOf(e.OriginalSource) Then tagArtiste.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Artiste")
        If tagTitre.IsAncestorOf(e.OriginalSource) Then tagTitre.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Titre")
        If tagAlbum.IsAncestorOf(e.OriginalSource) Then tagAlbum.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Album")
        If tagCompositeur.IsAncestorOf(e.OriginalSource) Then tagCompositeur.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Compositeur")
        If tagImage.IsAncestorOf(e.OriginalSource) Then tagImage.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Image")
        e.Handled = True
    End Sub
    Private Sub Recherche_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Recherche.Click
        Dim Arg As RequeteRechercheEventArgs = New RequeteRechercheEventArgs
        Arg.Artiste = InfosFichier.Artiste
        Arg.Titre = InfosFichier.Titre
        Arg.ID = InfosFichier.idRelease
        RaiseEvent RequeteRecherche(Me, Arg) 'InfosFichier.Artiste)
    End Sub
    Private Sub Extraction_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles Extraction.Click
        InfosFichier.Selected = True
        InfosFichier.ExtractionInfosTitre()
        InfosFichier.Selected = False
    End Sub

    'PERMET DE VALIDER LES MODIFICATIONS DES INFOS TAG
    Public Sub PlayerActivated()
        If InfosFichier IsNot Nothing Then InfosFichier.Selected = True
    End Sub
    Public Sub PlayerDeactivated()
        If InfosFichier IsNot Nothing Then InfosFichier.Selected = False
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DE LA MISE A JOUR DES INFORMATIONS--------------------
    '***********************************************************************************************
    Private Delegate Sub NoArgDelegate()
    'PROCEDURE DE MISE A JOUR DES INFORMATIONS DU FICHIER
    'Traitement du message lors de la notification de modification d'un fichier
    Private Sub ShellWatcher_Changed(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles ShellWatcher.Changed
        If Not BibliothequeLiee.MiseAJourOk Then
            NotifyShellWatcherFilesChanged(e, Nothing)
        End If
    End Sub
    'Traitement du message lors de la notification de renommage d'un fichier
    Private Sub ShellWatcher_Renamed(ByVal sender As Object, ByVal e As System.IO.RenamedEventArgs) Handles ShellWatcher.Renamed
        If Not BibliothequeLiee.MiseAJourOk Then
            NotifyShellWatcherFilesRenamed(e, Nothing)
        End If
    End Sub

    Public Sub NotifyShellWatcherFilesChanged(ByVal e As System.IO.FileSystemEventArgs, ByVal Infos As tagID3FilesInfos) Implements iNotifyShellUpdate.NotifyShellWatcherFilesChanged
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New NoArgDelegate(Sub()
                                                           If InfosFichier.NomComplet = e.FullPath Then
                                                               If Infos.ListeFichiers.Count > 1 Then
                                                                   For Each i In Infos.ListeFichiers
                                                                       If i = InfosFichier.NomComplet Then
                                                                           Infos = New tagID3FilesInfos(i)
                                                                           InfosFichier.Update(Infos)
                                                                       End If
                                                                   Next
                                                               Else
                                                                   If Infos Is Nothing Then Infos = New tagID3FilesInfos(e.FullPath)
                                                                   InfosFichier.Update(Infos)
                                                               End If
                                                           End If
                                                       End Sub))
    End Sub
    Public Sub NotifyShellWatcherFilesCreated(ByVal e As System.IO.FileSystemEventArgs, ByVal Infos As tagID3FilesInfos) Implements iNotifyShellUpdate.NotifyShellWatcherFilesCreated
    End Sub
    Public Sub NotifyShellWatcherFilesDeleted(ByVal e As System.IO.FileSystemEventArgs) Implements iNotifyShellUpdate.NotifyShellWatcherFilesDeleted
    End Sub
    Public Sub NotifyShellWatcherFilesRenamed(ByVal e As System.IO.RenamedEventArgs, ByVal Infos As tagID3FilesInfos) Implements iNotifyShellUpdate.NotifyShellWatcherFilesRenamed
        If Path.GetExtension(e.FullPath) = ".mp3" Then
            If Path.GetExtension(e.OldFullPath) = ".~p3" And (Path.GetFileNameWithoutExtension(e.OldFullPath) = Path.GetFileNameWithoutExtension(e.FullPath)) Then
                NotifyShellWatcherFilesChanged(e, Infos)
            Else
                Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                             New NoArgDelegate(Sub()
                                                                   If InfosFichier.NomComplet = e.OldFullPath Then
                                                                       NomFichierEnCours = e.FullPath
                                                                       UpdateFilesInfos(Infos)
                                                                   End If
                                                               End Sub))
            End If
        End If
    End Sub
    '***********************************************************************************************
    '---------------------------------GESTION DES MESSAGES DU MIXER---------------------------------
    '***********************************************************************************************
    Delegate Sub TraitementMsgMixer()
    Private Sub Mixer_BeforePlay(ByVal IDLine As String, ByVal Filename As String) Handles LineEnCours.BeforePlay
        If IDLine = IdPiste Then Debug.Print("before")
    End Sub
    Private Sub Mixer_AfterPlay(ByVal IDLine As String, ByVal Filename As String) Handles LineEnCours.AfterPlay
        If IDLine = IdPiste Then Debug.Print("after")
    End Sub
    Private Sub Mixer_MiseAJourAffichage(ByVal IDLine As String, ByVal PositionActuelle As Integer, ByVal DureeTotale As Integer,
                                    ByVal iGauche As Integer, ByVal iDroite As Integer, ByVal bpm As Single) Handles LineEnCours.MiseAJourAffichage
        'If IDLine = IdPiste Then
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                                    New TraitementMsgMixer(Sub()
                                                                               If Etat = EtatPlayer.Play Then
                                                                                   Gauche.Value = (iGauche * LineEnCours.Volume) / 100
                                                                                   Droite.Value = (iDroite * LineEnCours.Volume) / 100
                                                                                   PlayerPosition.Maximum = DureeTotale
                                                                                   Dim MinutesT As Integer = Int(DureeTotale / 60)
                                                                                   Dim SecondesT As Integer = Int((DureeTotale) - MinutesT * 60)
                                                                                   Dim Minutes As Integer = Int(PositionActuelle / 60)
                                                                                   Dim Secondes As Integer = Int((PositionActuelle) - Minutes * 60)
                                                                                   TempsT.Text = MinutesT.ToString("00") & " : " & SecondesT.ToString("00")
                                                                                   If EnchainementMorceaux.IsChecked Then
                                                                                       If ((Not DemandeFichierSuivantEnCours) And (IDLine = LineEnCours.IDMixerLine) And (PositionActuelle > DureeTotale - 10)) Then
                                                                                           DemandeFichierSuivantEnCours = True
                                                                                           RaiseEvent RequeteFichierSuivant(Me, Nothing)
                                                                                       End If
                                                                                   Else
                                                                                       If ((Not DemandeFichierSuivantEnCours) And (IDLine = LineEnCours.IDMixerLine) And (PositionActuelle > DureeTotale - 1)) Then
                                                                                           DemandeFichierSuivantEnCours = True
                                                                                           RaiseEvent RequeteFichierSuivant(Me, Nothing)
                                                                                       End If
                                                                                   End If
                                                                                   If bpm < 0 Then
                                                                                       tagBpm.Text = Math.Abs(bpm).ToString("00.0")
                                                                                       BpmGo.Content = "Bpm"
                                                                                       BpmGo.IsEnabled = True
                                                                                   Else
                                                                                       If (bpm <> 0 And (Not BpmGo.IsEnabled)) Then BpmGo.Content = bpm.ToString("00.0")
                                                                                   End If
                                                                                   ' BpmAff.Text = bpm.ToString("00.0 bpm")
                                                                                   If Not BlockageMaJPosition Then
                                                                                       Temps.Text = Minutes.ToString("00") & " : " & Secondes.ToString("00")
                                                                                       PlayerPosition.Value = CDbl(PositionActuelle)
                                                                                   End If
                                                                               End If
                                                                           End Sub))
    End Sub
    Private Sub Mixer_FinTitreAtteinte(ByVal IDLine As String, ByVal Filename As String) Handles LineEnCours.FinTitreAtteinte
        'If IDLine = IdPiste Then
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send,
                                 New TraitementMsgMixer(Sub()
                                                            '   If Not EnchainementMorceaux.IsChecked Then RaiseEvent RequeteFichierSuivant(Me, Nothing)
                                                        End Sub))
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                  New TraitementMsgMixer(Sub()
                                                             If Filename = IDTacheEnCours Then
                                                                 Temps.Text = "00 : 00"
                                                                 Gauche.Value = 0
                                                                 Droite.Value = 0
                                                                 PlayerPosition.Value = 0
                                                                 imBpPlay.Source = GetBitmapImage("../Images/imgboutons/play24.png")
                                                                 Etat = EtatPlayer.Arret
                                                                 IDTacheEnCours = ""
                                                                 IDFichierEnCours = ""
                                                                 ArretAnimationVinyl()
                                                             End If
                                                         End Sub))
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DU MIXAGE ENTRE MORCEAUX------------------------------
    '***********************************************************************************************
    Private Sub Mixage_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles Mixage.Elapsed
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                         New TraitementMsgMixer(Sub()
                                                                    If LinePrecedente Is Nothing Then
                                                                        Mixage.Stop()
                                                                        Exit Sub
                                                                    End If
                                                                    If Not EnchainementMorceaux.IsChecked Then
                                                                        LineEnCours.Volume = PlayerVolume.Value
                                                                        LinePrecedente.Volume = 0
                                                                        LinePrecedente.StopPlay()
                                                                        Mixage.Stop()
                                                                    Else
                                                                        If LineEnCours.Volume < PlayerVolume.Value Then
                                                                            LineEnCours.Volume = LineEnCours.Volume + PenteMixage * 2
                                                                            Debug.Print(LineEnCours.Volume)
                                                                        End If
                                                                        If (LineEnCours.Volume > (PlayerVolume.Value * PointMixage)) Then
                                                                            LinePrecedente.Volume = LinePrecedente.Volume - PenteMixage / 2
                                                                        End If
                                                                        If (LinePrecedente.Volume <= 0) Then
                                                                            LinePrecedente.StopPlay()
                                                                            Mixage.Stop()
                                                                        End If
                                                                    End If
                                                                End Sub))

    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DE L'EQUALIZER----------------------------------------
    '***********************************************************************************************
    Private Sub EqualChangeValue(ByVal sender As Object, ByVal e As System.Windows.RoutedPropertyChangedEventArgs(Of Double))
        If TypeOf e.OriginalSource Is EqualSlider Then
            If LineEnCours.IsOn Then
                '    LineEnCours.SetEqualizerGain(ExtraitChaine(CType(e.OriginalSource, EqualSlider).Name, "EquaSlider", "", 10), e.NewValue)
            End If
        End If
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION ANIMATION VINYL---------------------------------------
    '***********************************************************************************************
    Private Sub DemarrageAnimationVinyl()
        Dim Animation As DoubleAnimation = New DoubleAnimation
        Animation.To = 360
        Animation.From = 0
        Animation.Duration = New Duration(New TimeSpan(0, 0, 1.81))
        Animation.RepeatBehavior = RepeatBehavior.Forever
        RotationImage.BeginAnimation(RotateTransform.AngleProperty, Animation)
    End Sub
    Private Sub ArretAnimationVinyl()
        RotationImage.BeginAnimation(RotateTransform.AngleProperty, Nothing)
    End Sub
    '***********************************************************************************************
    '---------------------------------FONCTION UTILITAIRE-------------------------------------------
    '***********************************************************************************************
    Private Function GetBitmapImage(ByVal NomImage As String) As BitmapImage
        Dim bi3 As New BitmapImage
        bi3.BeginInit()
        bi3.UriSource = New Uri(NomImage, UriKind.Relative)
        bi3.EndInit()
        Return bi3
    End Function

End Class
Public Class RequeteRechercheEventArgs
    Inherits EventArgs

    Public Property Artiste As String
    Public Property Titre As String
    Public Property ID As String
End Class