'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 20/07/10 rev 04/08/10
'DESCRIPTION :Classe de lecteur audio
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
Public Class WindowPlayer2
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
    Event FichierSuivant(ByVal NumPiste As Integer)
    Event FichierCharge(ByVal NomFichier As String)
    Public Event RequeteRecherche(ByVal ChaineRecherche As String)
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES VARIABLES DE LA FENETRE
    Private WithEvents Mixer As dxMixer
    Private WithEvents RafraicheAffichage As Timers.Timer = New Timers.Timer
    Private Etat As EtatPlayer = EtatPlayer.Arret
    Private RepDest As String
    Private NomFichierEnCours As String
    Private FichierTempEnCours As String
    Private FichierTempAEffacer As String
    Private BlockageMaJPosition As Boolean
    Private WaveOctetParSec As Integer
    Private InfosFichier As tagID3FilesInfosDO
    Private WithEvents ShellWatcher As New System.IO.FileSystemWatcher
    Private GBPlayerInstalle As Boolean
    '***********************************************************************************************
    '---------------------------------INITIALISATION DE LA CLASSE-----------------------------------
    '***********************************************************************************************
    Sub New(ByVal FenetreParent As Window)
        ' Cet appel est requis par le concepteur.
        InitializeComponent()
        Owner = FenetreParent
        RafraicheAffichage.Interval = 50
        RepDest = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) & "\" & GBAU_NOMDOSSIER_TEMP
        If Not Directory.Exists(RepDest) Then Directory.CreateDirectory(RepDest)
        PlayerPosition.IsEnabled = False
        Lecteur.LoadedBehavior = MediaState.Manual
        Lecteur.UnloadedBehavior = MediaState.Stop
    End Sub

    Private Sub WindowPlayer_Initialized(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Initialized
        Me.Height = 0
        Dim ConfigUtilisateur As ConfigPerso = New ConfigPerso
        ConfigUtilisateur = ConfigPerso.LoadConfig
        PlayerVolume.Value = CDbl(ConfigUtilisateur.PLAYERVOLUME0)
    End Sub
    Private Sub WindowPlayer_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Loaded
        OuvertureLecteur()
        If System.Environment.OSVersion.Platform = PlatformID.Win32NT Then
            If System.Environment.OSVersion.Version.Major > 5 Then PlateformVista = True
        End If
    End Sub
    Private Sub OuvertureLecteur()
        ' Dim BordureLecteur = CType(Owner, MainWindow).BordureLecteur
        ' Dim HauteurPlayer As Double = 200
        ' If BordureLecteur IsNot Nothing Then
        ' Me.Left = Me.PointToScreen(BordureLecteur.TranslatePoint(New Point(0, 0), Me)).X
        ' Dim AnimationPosition As DoubleAnimation = New DoubleAnimation()
        ' AnimationPosition.From = Me.PointToScreen(BordureLecteur.TranslatePoint(New Point(0, 0), Me)).Y
        ' AnimationPosition.To = Me.PointToScreen(BordureLecteur.TranslatePoint(New Point(0, 0), Me)).Y - HauteurPlayer
        ' AnimationPosition.Duration = New Duration(New TimeSpan(0, 0, 2))
        ' AnimationPosition.FillBehavior = FillBehavior.Stop
        ' Me.BeginAnimation(WindowPlayer.TopProperty, AnimationPosition)
        ' Dim AnimationHauteur As DoubleAnimation = New DoubleAnimation()
        ' AnimationHauteur.From = 0
        ' AnimationHauteur.To = HauteurPlayer
        ' AnimationHauteur.Duration = New Duration(New TimeSpan(0, 0, 2))
        ' AnimationHauteur.FillBehavior = FillBehavior.Stop
        ' AddHandler AnimationHauteur.Completed, AddressOf Event_OuvertureLecteurTerminee
        ' Me.BeginAnimation(WindowPlayer.HeightProperty, AnimationHauteur)
        ' End If
    End Sub
    Private Sub Event_OuvertureLecteurTerminee(ByVal sender As Object, ByVal e As EventArgs)
        Dim BordureLecteur = CType(Owner, MainWindow).BordureLecteur
        BordureLecteur.Height = Me.Height
    End Sub
    '***********************************************************************************************
    '---------------------------------DESTRUCTION DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Protected Overrides Sub Finalize()
        Try
            Thread.Sleep(200)
            For Each i In FileIO.FileSystem.GetFiles(RepDest)
                FileIO.FileSystem.DeleteFile(i)
            Next i
            MyBase.Finalize()
        Catch ex As Exception

        End Try
    End Sub
    Private Sub WindowPlayer_Closed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Closed
        'BoucleLecture.Stop()
        RafraicheAffichage.Stop()
        If Mixer IsNot Nothing Then Mixer.RemoveAllLines()
        Mixer = Nothing
    End Sub

    'PROCEDURE DE FERMETURE DU LECTEUR APPELE PAR LA FENETRE MAITRE
    Sub ClosePlayer(ByRef Config As ConfigPerso)
        If Config IsNot Nothing Then Config.PLAYERVOLUME0 = CStr(PlayerVolume.Value)
        Me.Close()
    End Sub

    '**************************************************************************************************************
    '**************************************GESTION DU DRAG AND DROP************************************************
    '**************************************************************************************************************
    Dim StartPoint As Point
    Dim TypeObjetClic As Object
    Dim PlateformVista As Boolean
    Private Sub WindowPlayer_PreviewMouseLeftButtonUp(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonUp
        StartPoint = New Point
    End Sub
    Private Sub WindowPlayer_PreviewMouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonDown
        StartPoint = e.GetPosition(tagImage)
        TypeObjetClic = e.OriginalSource
    End Sub
    Private Sub WindowPlayer_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles Me.MouseLeave
        StartPoint = New Point
    End Sub
    Private Sub WindowPlayer_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles Me.MouseMove
        If TypeOf (e.OriginalSource) Is Image And TypeOf (TypeObjetClic) Is Image Then
            'Dim DonneeSurvolee As tagID3FilesInfosDO = InfosFichier
            'If (CType(e.OriginalSource, Image).Name) = "tagImage" Then
            '    Dim MousePos As Point = e.GetPosition(tagImage)
            '    Dim Diff As Vector = StartPoint - MousePos
            '    If StartPoint.X <> 0 And StartPoint.Y <> 0 And (e.LeftButton = MouseButtonState.Pressed And Math.Abs(Diff.X) > 2 Or
            '                                                    Math.Abs(Diff.Y) > 2) Then
            '    Dim BitmapData As MemoryStream
            '    Using NewTagId3 As TagID3.tagID3Object = New TagID3.tagID3Object(DonneeSurvolee.NomComplet)
            ' BitmapData = TagID3.tagID3Object.FonctionUtilite.ConvertJpegdataToDibstream(NewTagId3.ID3v2_ImageData)
            ' End Using
            ' If PlateformVista Then
            ' Dim TabParametres(0) As KeyValuePair(Of String, Object)
            ' TabParametres(0) = New KeyValuePair(Of String, Object)(DataFormats.Dib, BitmapData)
            ' DragSourceHelper.DoDragDrop(CType(e.OriginalSource, Image), e.GetPosition(e.OriginalSource),
            '                            DragDropEffects.Copy, TabParametres)
            'Else
            '    Dim data As DataObject = New DataObject()
            '    data.SetData(DataFormats.Dib, BitmapData)
            '    Dim effects As DragDropEffects = DragDrop.DoDragDrop(Me, data, DragDropEffects.Copy)
            'End If
            'e.Handled = True
            'End If
            'End If
        ElseIf TypeOf (e.OriginalSource) Is Border And TypeOf (TypeObjetClic) Is Border Then
            If CType(e.OriginalSource, Border).Name = "EnteteLecteur" Then
                Dim MousePos As Point = e.GetPosition(EnteteLecteur)
                Dim Diff As Vector = StartPoint - MousePos
                If StartPoint.X <> 0 And StartPoint.Y <> 0 And (e.LeftButton = MouseButtonState.Pressed And Math.Abs(Diff.X) > 2 Or
                                                                Math.Abs(Diff.Y) > 2) Then
                    Dim FilesDataObject(0) As String
                    Dim FilesTextName As String = ""
                    FilesDataObject(0) = InfosFichier.NomComplet
                    FilesTextName = InfosFichier.Nom
                    If PlateformVista Then
                        Dim TabParametres(1) As KeyValuePair(Of String, Object)
                        TabParametres(0) = New KeyValuePair(Of String, Object)(DataFormats.FileDrop, FilesDataObject)
                        TabParametres(1) = New KeyValuePair(Of String, Object)(DataFormats.Text, FilesTextName)
                        DragSourceHelper.DoDragDrop(Me, e.GetPosition(Me), DragDropEffects.Copy, TabParametres)
                    Else
                        Dim data As DataObject = New DataObject()
                        data.SetData(DataFormats.FileDrop, FilesDataObject)
                        data.SetData(DataFormats.Text, FilesTextName)
                        Dim effects As DragDropEffects = DragDrop.DoDragDrop(Me, data, DragDropEffects.Copy)
                    End If
                    e.Handled = True
                End If
            End If
        End If
    End Sub

    Dim ActionCopierEnCours As Boolean
    Dim ActionCopierTagEnCours As Boolean
    Private Sub WindowPlayer_Drop(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.Drop
        Dim DonneeSurvolee As tagID3FilesInfosDO = InfosFichier
        If (e.Data.GetDataPresent(DataFormats.Dib)) And (DonneeSurvolee IsNot Nothing) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            If PlateformVista Then DropTargetHelper.Drop(e.Data, e.GetPosition(e.OriginalSource), e.Effects)
            FileDrop(e)
        ElseIf (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
            If (DonneeSurvolee IsNot Nothing) Then
                e.Effects = e.AllowedEffects And DragDropEffects.Copy
                If PlateformVista Then DropTargetHelper.Drop(e.Data, e.GetPosition(e.OriginalSource), e.Effects)
                TagId3Drop(e)
            Else
                e.Effects = DragDropEffects.None
            End If
        Else
            e.Effects = DragDropEffects.None
        End If
        e.Handled = True
    End Sub
    Private Sub WindowPlayer_DragEnter(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.DragEnter
        Dim FlagPlateformVista As Boolean
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        Dim DonneeSurvolee As tagID3FilesInfosDO = Nothing
        DonneeSurvolee = InfosFichier
        If (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
            Dim Origine As String = CType(e.Data.GetData(DataFormats.FileDrop), String())(0)
            If Path.GetExtension(Origine) = ".mp3" And (DonneeSurvolee IsNot Nothing) Then
                e.Effects = e.AllowedEffects And DragDropEffects.Copy
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                e.Data, e.GetPosition(e.OriginalSource),
                                                                e.Effects, "Copier les TAGID3 vers %1", DonneeSurvolee.Nom)
            Else
                e.Effects = DragDropEffects.None
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                e.Data, e.GetPosition(e.OriginalSource),
                                                                e.Effects, "", "")
            End If
            'e.Handled = True
        ElseIf (e.Data.GetDataPresent(DataFormats.Dib)) And (DonneeSurvolee IsNot Nothing) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                            e.Data, e.GetPosition(e.OriginalSource),
                                                            e.Effects, "Copier l'image du label vers %1", DonneeSurvolee.Nom)
        Else
            e.Effects = DragDropEffects.None
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                            e.Data, e.GetPosition(e.OriginalSource),
                                                            e.Effects, "", "")
            e.Handled = True
        End If
    End Sub
    Private Sub WindowPlayer_DragLeave(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.DragLeave
        If PlateformVista Then
            DropTargetHelper.DragLeave(e.Data)
        End If
        e.Handled = True
    End Sub
    Private Sub WindowPlayer_DragOver(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.DragOver
        Dim FlagPlateformVista As Boolean
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        Dim DonneeSurvolee As tagID3FilesInfosDO = Nothing
        DonneeSurvolee = InfosFichier
        If ((e.Data.GetDataPresent(DataFormats.FileDrop)) Or (e.Data.GetDataPresent(DataFormats.Dib))) And
            (DonneeSurvolee IsNot Nothing) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragOver(e.GetPosition(e.OriginalSource), e.Effects)
        Else
            e.Effects = DragDropEffects.None
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                e.Data, e.GetPosition(e.OriginalSource),
                                                                e.Effects, "", "")
        End If
        'Me.BringIntoView()
        e.Handled = True
    End Sub

    'FONCTION DE TRAITEMENT DU DROP SUR LA LISTE
    Private Sub FileDrop(ByVal e As System.Windows.DragEventArgs)
        Dim Formats As String() = e.Data.GetFormats()
        Dim Chemin As String = ""
        Dim NomDirectory As String = ""
        Dim NomFichier As String = ""
        If e.Data.GetDataPresent(DataFormats.Dib) Then
            Dim DonneeSurvolee As tagID3FilesInfosDO = InfosFichier
            Using NewTagId3 As TagID3.tagID3Object = New TagID3.tagID3Object(DonneeSurvolee.NomComplet, True)
                NewTagId3.ID3v2_SetImage(TagID3.tagID3Object.FonctionUtilite.ConvertDibstreamToJpegdata(
                                            CType(e.Data.GetData(DataFormats.Dib), System.IO.MemoryStream)), "Label")
            End Using
        End If
    End Sub
    'FONCTION DE TRAITEMENT DU DROP TAG SUR LA LISTE
    Private Sub TagId3Drop(ByVal e As System.Windows.DragEventArgs)
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            Dim Origine As String = CType(e.Data.GetData(DataFormats.FileDrop), String())(0)
            Dim Destination As String = InfosFichier.NomComplet
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
    '***********************************************************************************************
    '---------------------------------FONCTIONS PUBLIQUES DE LA CLASSE------------------------------
    '***********************************************************************************************
    Dim IDTacheEnCours As String
    Dim IDFichierEnCours As String
    Dim NumLineMixer As Integer = 0
    Public Function PlayFichier(ByVal NomFichier As String) As Boolean
        Try
            If NomFichier <> IDFichierEnCours Or Etat = EtatPlayer.Pause Then
                Dim FichierDiscAudio As Boolean = Path.GetExtension(NomFichier) = ".cd"
                If Mixer Is Nothing Then Mixer = New dxMixer(1)
                ' NumLineMixer += 1
                If Mixer.IsOn Then Mixer.AddLine(CStr(NumLineMixer))
                If Mixer.IsOn Then
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
                    End If
                    If Mixer.LineByName(CStr(NumLineMixer)).OpenLine(FichierTempEnCours) Then 'CHANGEMENT
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
                        If Not FichierDiscAudio Then FichierTempAEffacer = FichierTempEnCours
                        Mixer.LineByName(CStr(NumLineMixer)).Volume = PlayerVolume.Value
                        If Not FichierDiscAudio Then Mixer.LineByName(CStr(NumLineMixer)).StartBpmCalculate()
                        '    Mixer.Pan = BalanceG.gbValeur
                        '  UpdateVolume()
                        Call Mixer.LineByName(CStr(NumLineMixer)).Play()
                        GBPlayerInstalle = True
                        imBpPlay.Source = GetBitmapImage("../Images/Boutons/BpPlayTravail32.png")
                        imBpStop.Source = GetBitmapImage("../Images/Boutons/BpStopRepos32.png")
                        imBpPause.Source = GetBitmapImage("../Images/Boutons/BpPauseRepos32.png")
                        Etat = EtatPlayer.Play
                        UpdateFilesInfos()
                        DemarrageAnimationVinyl()
                        VinylImage.Visibility = Windows.Visibility.Visible
                        PlayerPosition.IsEnabled = True
                    End If
                End If
            End If
        Catch ex As Exception
            MsgBox(ex.Message)
            '    PlayFichierwpf(NomFichier)
        End Try
        Return True
    End Function
    Private Sub PlayFichierwpf(ByVal NomFichier As String)
        Dim FichierTemp As String
        'NomFichier = Path.ChangeExtension(NomFichier, ".mp3") 'A SUPPRIMER EEEEEEEEE
        FichierTemp = RepDest & Path.GetFileName(NomFichier)
        If Not File.Exists(FichierTemp) Then
            FileIO.FileSystem.CopyFile(NomFichier, FichierTemp)
            Dim info As FileInfo = New FileInfo(FichierTemp)
            info.IsReadOnly = False
        End If
        NomFichierEnCours = NomFichier
        If FichierTempEnCours <> FichierTemp Then Lecteur.Source = Nothing
        FichierTempEnCours = FichierTemp
        'If GBPlayerInstalle Then
        'If Mixer.IsOn Then
        'If Mixer.LineByName(CStr(0)).IsOn Then
        'Mixer.LineByName(CStr(0)).CloseLine()
        'End If
        'End If
        'End If
        If FichierTempAEffacer <> "" Then
            If File.Exists(FichierTempAEffacer) And FichierTempAEffacer <> FichierTempEnCours Then
                Try
                    FileIO.FileSystem.DeleteFile(FichierTempAEffacer)
                Catch ex As Exception
                End Try
            End If
        End If
        FichierTempAEffacer = FichierTempEnCours
        If Lecteur.Source Is Nothing Then Lecteur.Source = New Uri(FichierTempEnCours)
        Lecteur.Volume = PlayerVolume.Value / 100
        Lecteur.Play()
        GBPlayerInstalle = False
        imBpPlay.Source = GetBitmapImage("../Images/Boutons/BpPlayTravail32.png")
        imBpStop.Source = GetBitmapImage("../Images/Boutons/BpStopRepos32.png")
        imBpPause.Source = GetBitmapImage("../Images/Boutons/BpPauseRepos32.png")
        Etat = EtatPlayer.Play
        UpdateFilesInfos()
        DemarrageAnimationVinyl()
        VinylImage.Visibility = Windows.Visibility.Visible
        If Not RafraicheAffichage.Enabled Then RafraicheAffichage.Start()
        PlayerPosition.IsEnabled = True
    End Sub
    Private Sub UpdateFilesInfos()
        InfosFichier = CType(FindResource("InfosFichier"), tagID3FilesInfosDO)
        InfosFichier.Update(New tagID3FilesInfos(NomFichierEnCours), CType(Owner, MainWindow))
        If File.Exists(NomFichierEnCours) Then
            ShellWatcher.IncludeSubdirectories = True ' False
            ShellWatcher.Path = CType(Owner, MainWindow).ListeRepertoires.gbRacine ' Path.GetDirectoryName(NomFichierEnCours)
            ShellWatcher.Filter = "*.mp3" ' Path.GetFileName(NomFichierEnCours)
            ShellWatcher.NotifyFilter = NotifyFilters.LastWrite Or NotifyFilters.FileName
            ShellWatcher.EnableRaisingEvents = True 'A desactiver à la fin de la lecture
        End If
        InfosFichier.Selected = True
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION INTERFACE---------------------------------------------
    '***********************************************************************************************
    Private Sub BPPlay_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        PlayFichier(NomFichierEnCours)
    End Sub
    Private Sub BPPlay_PreviewMouseDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles BPPlay.PreviewMouseDown
        If Etat <> EtatPlayer.Play Then
            imBpPlay.Source = GetBitmapImage("../Images/Boutons/BpPlayTravail32.png")
            BPPlay.CaptureMouse()
        End If
    End Sub
    Private Sub BPPlay_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles BPPlay.MouseLeave
        If Etat <> EtatPlayer.Play Then imBpPlay.Source = GetBitmapImage("../Images/Boutons/BpPlayRepos32.png")
    End Sub

    Private Sub BPPause_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        If GBPlayerInstalle Then
            If Mixer.IsOn Then
                If Mixer.LineByName(CStr(NumLineMixer)).IsOn Then
                    Mixer.LineByName(CStr(NumLineMixer)).PausePlay()
                    imBpPlay.Source = GetBitmapImage("../Images/Boutons/BpPlayRepos32.png")
                    Etat = EtatPlayer.Pause
                    ArretAnimationVinyl()
                    'BoucleLecture.Enabled = False
                    '     RafraicheAffichage.Enabled = False

                    Droite.Value = 0
                    Gauche.Value = 0
                End If
            End If
        Else
            Lecteur.Pause()
            imBpPlay.Source = GetBitmapImage("../Images/Boutons/BpPlayRepos32.png")
            Etat = EtatPlayer.Pause
            ArretAnimationVinyl()
        End If
    End Sub
    Private Sub BPPause_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles BPPause.MouseLeave
        If Etat <> EtatPlayer.Pause Then imBpPause.Source = GetBitmapImage("../Images/Boutons/BpPauseRepos32.png")
    End Sub
    Private Sub BPPause_PreviewMouseDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles BPPause.PreviewMouseDown
        If Etat = EtatPlayer.Play Then
            imBpPause.Source = GetBitmapImage("../Images/Boutons/BpPauseTravail32.png")
        End If
    End Sub

    Private Sub BPStop_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        If GBPlayerInstalle Then
            If Mixer.IsOn Then
                If Mixer.LineByName(CStr(NumLineMixer)).IsOn Then
                    Mixer.LineByName(CStr(NumLineMixer)).CloseLine()
                    imBpPlay.Source = GetBitmapImage("../Images/Boutons/BpPlayRepos32.png")
                    imBpStop.Source = GetBitmapImage("../Images/Boutons/BpStopRepos32.png")
                    Etat = EtatPlayer.Arret
                    ArretAnimationVinyl()
                    'BoucleLecture.Enabled = False
                    RafraicheAffichage.Enabled = False
                    PlayerPosition.IsEnabled = False
                    PlayerPosition.Value = 0
                    Droite.Value = 0
                    Gauche.Value = 0
                    Temps.Text = "00 : 00"
                End If
            End If
        Else
            Lecteur.Stop()
            imBpPlay.Source = GetBitmapImage("../Images/Boutons/BpPlayRepos32.png")
            imBpStop.Source = GetBitmapImage("../Images/Boutons/BpStopRepos32.png")
            Etat = EtatPlayer.Arret
            ArretAnimationVinyl()
            PlayerPosition.IsEnabled = False
        End If
        imBpPause.Source = GetBitmapImage("../Images/Boutons/BpPauseRepos32.png")
    End Sub
    Private Sub BPStop_PreviewMouseDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles BPStop.PreviewMouseDown
        If Etat = EtatPlayer.Play Or Etat = EtatPlayer.Pause Then
            imBpStop.Source = GetBitmapImage("../Images/Boutons/BpStopTravail32.png")
        End If
    End Sub
    Private Sub BPStop_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles BPStop.MouseLeave
        If Etat <> EtatPlayer.Arret Then imBpStop.Source = GetBitmapImage("../Images/Boutons/BpStopRepos32.png")
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
        If GBPlayerInstalle Then
            If Mixer.IsOn Then
                If Mixer.LineByName(CStr(NumLineMixer)).IsOn Then
                    Mixer.LineByName(CStr(NumLineMixer)).Volume = e.NewValue
                End If
            End If
        Else
            Lecteur.Volume = e.NewValue / 100
        End If
    End Sub

    'TRAITEMENT DE LA BARRE D'AVANCEMENT
    Private Sub PlayerPosition_PreviewMouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles PlayerPosition.PreviewMouseLeftButtonDown
        BlockageMaJPosition = True
    End Sub
    Private Sub PlayerPosition_PreviewMouseLeftButtonUp(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles PlayerPosition.PreviewMouseLeftButtonUp
        If GBPlayerInstalle Then
            If Not Mixer Is Nothing Then
                If Mixer.IsOn Then
                    If Mixer.LineExist(CStr(NumLineMixer)) Then
                        If TypeOf (e.OriginalSource) Is Thumb Then
                            Mixer.LineByName(CStr(NumLineMixer)).Position = PlayerPosition.Value
                        Else
                            Mixer.LineByName(CStr(NumLineMixer)).Position = ((PlayerPosition.Maximum - PlayerPosition.Minimum) / PlayerPosition.ActualWidth * e.GetPosition(PlayerPosition).X)
                        End If
                        BlockageMaJPosition = False
                    End If
                End If
            End If
        Else
            If TypeOf (e.OriginalSource) Is Thumb Then
                Lecteur.Position = New TimeSpan(0, 0, CInt(PlayerPosition.Value))
            Else
                Lecteur.Position = New TimeSpan(0, 0, CInt((PlayerPosition.Maximum - PlayerPosition.Minimum) / PlayerPosition.ActualWidth * e.GetPosition(PlayerPosition).X))
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
    Private Async Sub Player2_PreviewMouseRightButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseRightButtonDown
        If tagArtiste.IsAncestorOf(e.OriginalSource) Then tagArtiste.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Artiste")
        If tagTitre.IsAncestorOf(e.OriginalSource) Then tagTitre.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Titre")
        If tagAlbum.IsAncestorOf(e.OriginalSource) Then tagAlbum.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Album")
        If tagCompositeur.IsAncestorOf(e.OriginalSource) Then tagCompositeur.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Compositeur")
        If tagImage.IsAncestorOf(e.OriginalSource) Then tagImage.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Image")
        e.Handled = True
    End Sub

    Private Sub Recherche_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        RaiseEvent RequeteRecherche(InfosFichier.Artiste)
    End Sub

    Private Sub Extraction_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)

    End Sub

    'PERMET DE VALIDER LES MODIFICATIONS DES INFOS TAG
    Private Sub clsPlayer2_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated
        If InfosFichier IsNot Nothing Then InfosFichier.Selected = True
    End Sub
    Private Sub clsPlayer2_Deactivated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Deactivated
        If InfosFichier IsNot Nothing Then InfosFichier.Selected = False
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DE LA MISE A JOUR DES INFORMATIONS--------------------
    '***********************************************************************************************
    'PROCEDURE DE MISE A JOUR DES INFORMATIONS DU FICHIER
    'Traitement du message lors de la notification de creation d'un fichier
    Delegate Sub CallBackToFileSystemWatcher(ByVal e As FileSystemEventArgs)
    'Traitement du message lors de la notification de modification d'un fichier
    Private Sub ShellWatcher_Changed(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles ShellWatcher.Changed
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                  New CallBackToFileSystemWatcher(AddressOf NotifyShellWatcherChanged), e)
    End Sub
    Private Sub NotifyShellWatcherChanged(ByVal e As FileSystemEventArgs)
        If InfosFichier.NomComplet = e.FullPath Then
            InfosFichier.Update(New tagID3FilesInfos(e.FullPath))
        End If
    End Sub
    'Traitement du message lors de la notification de renommage d'un fichier
    Delegate Sub CallBackToFileSystemWatcherRenamed(ByVal e As System.IO.RenamedEventArgs)
    Private Sub ShellWatcher_Renamed(ByVal sender As Object, ByVal e As System.IO.RenamedEventArgs) Handles ShellWatcher.Renamed
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                                  New CallBackToFileSystemWatcherRenamed(AddressOf NotifyShellWatcherRenamed), e)
    End Sub
    Private Sub NotifyShellWatcherRenamed(ByVal e As System.IO.RenamedEventArgs)
        If Path.GetExtension(e.FullPath) = ".mp3" Then
            If Path.GetExtension(e.OldFullPath) = ".~p3" And (Path.GetFileNameWithoutExtension(e.OldFullPath) = Path.GetFileNameWithoutExtension(e.FullPath)) Then
                NotifyShellWatcherChanged(e)
            Else
                If InfosFichier.NomComplet = e.OldFullPath Then
                    NomFichierEnCours = e.FullPath
                    UpdateFilesInfos()
                End If
            End If
        End If
    End Sub

    'REPONSE AU MESSAGE DE MISE A JOUR DE L'AFFICHAGE
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
    '---------------------------------GESTION DES MESSAGES DU MIXER---------------------------------
    '***********************************************************************************************
    Delegate Sub TraitementMsgMixer()
    Private Sub Mixer_BeforePlay(ByVal IDLine As String, ByVal Filename As String) Handles Mixer.BeforePlay
        Debug.Print("before")
    End Sub
    Private Sub Mixer_AfterPlay(ByVal IDLine As String, ByVal Filename As String) Handles Mixer.AfterPlay
        Debug.Print("after")
    End Sub
    Private Sub Mixer_MiseAJourAffichage(ByVal IDLine As String, ByVal PositionActuelle As Integer, ByVal DureeTotale As Integer,
                                    ByVal iGauche As Integer, ByVal iDroite As Integer, ByVal bpm As Single) Handles Mixer.MiseAJourAffichage
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                  New TraitementMsgMixer(Sub()
                                                             Debug.Print(IDLine)
                                                             Gauche.Value = iGauche
                                                             Droite.Value = iDroite
                                                             PlayerPosition.Maximum = DureeTotale
                                                             Dim MinutesT As Integer = Int(DureeTotale / 60)
                                                             Dim SecondesT As Integer = Int((DureeTotale) - MinutesT * 60)
                                                             Dim Minutes As Integer = Int(PositionActuelle / 60)
                                                             Dim Secondes As Integer = Int((PositionActuelle) - Minutes * 60)
                                                             TempsT.Text = MinutesT.ToString("00") & " : " & SecondesT.ToString("00")
                                                             BpmAff.Text = bpm.ToString("00.00")
                                                             If Not BlockageMaJPosition Then
                                                                 Temps.Text = Minutes.ToString("00") & " : " & Secondes.ToString("00")
                                                                 PlayerPosition.Value = CDbl(PositionActuelle)
                                                             End If
                                                         End Sub))
    End Sub
    Private Sub Mixer_FinTitreAtteinte(ByVal IDLine As String, ByVal Filename As String) Handles Mixer.FinTitreAtteinte
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                  New TraitementMsgMixer(Sub()
                                                             If Filename = IDTacheEnCours Then
                                                                 Temps.Text = "00 : 00"
                                                                 Gauche.Value = 0
                                                                 Droite.Value = 0
                                                                 PlayerPosition.Value = 0
                                                                 imBpPlay.Source = GetBitmapImage("../Images/Boutons/BpPlayRepos32.png")
                                                                 Etat = EtatPlayer.Arret
                                                                 IDTacheEnCours = ""
                                                                 IDFichierEnCours = ""
                                                                 ArretAnimationVinyl()
                                                             End If
                                                         End Sub))
    End Sub
    '***********************************************************************************************
    '---------------------------------GESTION DES MESSAGES DU LECTEUR MEDIAPLAYER WPF---------------
    '***********************************************************************************************
    Private Sub Lecteur_Progress(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles RafraicheAffichage.Elapsed
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New TraitementMsgMixer(Sub()
                                                                If Not GBPlayerInstalle Then
                                                                    If Lecteur.NaturalDuration.HasTimeSpan Then
                                                                        PlayerPosition.Maximum = Lecteur.NaturalDuration.TimeSpan.TotalSeconds
                                                                        If Not BlockageMaJPosition Then PlayerPosition.Value = Lecteur.Position.TotalSeconds
                                                                        Dim MinutesT As Integer = Int(PlayerPosition.Maximum / 60)
                                                                        Dim SecondesT As Integer = Int(PlayerPosition.Maximum - MinutesT * 60)
                                                                        Dim Minutes As Integer = Int(PlayerPosition.Value / 60)
                                                                        Dim Secondes As Integer = Int(PlayerPosition.Value - Minutes * 60)
                                                                        Temps.Text = Minutes.ToString("00") & " : " & Secondes.ToString("00")
                                                                        TempsT.Text = MinutesT.ToString("00") & " : " & SecondesT.ToString("00")
                                                                    End If
                                                                End If
                                                                If WaveOctetParSec > 0 Then
                                                                    Dim Minutes As Integer = Int(PlayerPosition.Value / WaveOctetParSec / 60)
                                                                    Dim Secondes As Integer = Int((PlayerPosition.Value / WaveOctetParSec) - Minutes * 60)
                                                                    Temps.Text = Minutes.ToString("00") & " : " & Secondes.ToString("00")
                                                                End If
                                                            End Sub))
    End Sub
    Private Sub Lecteur_MediaEnded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Lecteur.MediaEnded
        Lecteur.Stop()
        imBpPlay.Source = GetBitmapImage("../Images/Boutons/BpPlayRepos.png")
        Etat = EtatPlayer.Arret
        ArretAnimationVinyl()
        Lecteur.Source = Nothing
        PlayerPosition.Value = 0
        Try
            If File.Exists(FichierTempEnCours) Then FileIO.FileSystem.DeleteFile(FichierTempEnCours)
        Catch ex As Exception
        End Try
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

