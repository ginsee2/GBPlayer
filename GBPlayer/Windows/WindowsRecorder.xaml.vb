'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 22/04/2013
'DESCRIPTION :Fenetre d'enregistrement audio
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Option Compare Text

Imports System.Windows.Controls.Primitives
Imports System.IO
Imports System.Windows.Media.Animation
Imports System.Threading
Imports System
Imports System.Collections.ObjectModel
Imports System.Runtime.InteropServices
Imports gbDev.fileWave

'***********************************************************************************************
'---------------------------------TYPES PUBLIC DE LA CLASSE------------------------------------
'***********************************************************************************************
Public Class WindowsRecorder
    '***********************************************************************************************
    '---------------------------------CONSTANTES PRIVEES DE LA CLASSE-------------------------------
    '***********************************************************************************************
    Private Const GBAU_NOMDOSSIER_TEMP = "GBDev\GBPlayer\TempRecorder\"
    Private Enum EtatRecorder As Integer
        Arret = 0
        Play = 1
        Pause = 2
        Record = 3
    End Enum
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES EVENEMENTS DU CONTROLE
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES VARIABLES DE LA FENETRE
    Private WithEvents TheMixer As dxMixer
    Private RepDest As String
    Private NumLineMixer As Integer = 100
    Private AnimationEnCours As Boolean
    Private BibliothequeLiee As tagID3Bibliotheque
    Private _DirectXSystemList As New ObservableCollection(Of String)
    Public ReadOnly Property DirectXSystemList As ObservableCollection(Of String)
        Get
            Return _DirectXSystemList
        End Get
    End Property
    Public ReadOnly Property IsOk As Boolean
        Get
            'If ListePistes.Count > 0 Then Return True
            If TheMixer IsNot Nothing Then If TheMixer.IsOn Then Return True
            Return False
        End Get
    End Property
    Private DeviceEnCours As Integer
    Private DeviceOccupe As Boolean
    Private hRecord As IntPtr
    Private WithEvents RafraicheAffichage As Timers.Timer = New Timers.Timer
    Private vRecordProc As RECORDPROC = AddressOf RECORDPROCInterne
    Private InputType As String
    Private Etat As EtatRecorder = EtatRecorder.Arret
    '***********************************************************************************************
    '---------------------------------INITIALISATION DE LA CLASSE-----------------------------------
    '***********************************************************************************************
    Sub New(ByVal FenetreParent As Window, ByVal Bibliotheque As tagID3Bibliotheque, ByVal MixerEnCours As dxMixer)
        ' Cet appel est requis par le concepteur.
        InitializeComponent()
        TheMixer = MixerEnCours
        Owner = FenetreParent
        BibliothequeLiee = Bibliotheque
        RepDest = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) & "\" & GBAU_NOMDOSSIER_TEMP
        If Not Directory.Exists(RepDest) Then Directory.CreateDirectory(RepDest)
        RafraicheAffichage.Interval = 10
    End Sub
    Private Sub WindowPlayer_Initialized(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Initialized
        Me.Height = 0
        'Dim ConfigUtilisateur As ConfigPerso = New ConfigPerso
        'ConfigUtilisateur = ConfigPerso.LoadConfig
        ' PlayerVolume.Value = CDbl(ConfigUtilisateur.PLAYERVOLUME0)
    End Sub
    Private Sub WindowPlayer_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Loaded
        Visibility = Visibility.Collapsed
        If TheMixer IsNot Nothing Then
            Dim DeviceCount As Integer
            Dim RecordInfos As BASS_DEVICEINFO = New BASS_DEVICEINFO
            While (BASS_RecordGetDeviceInfo(DeviceCount, RecordInfos) <> 0)
                If (RecordInfos.Flags And Enum_Bass_Deviceinfo_Flags.BASS_DEVICE_ENABLED) > 0 Then   
                    _DirectXSystemList.Add("<" & DeviceCount & ">- " & Marshal.PtrToStringAnsi(RecordInfos.Name))
                    DeviceCount += 1
                End If
            End While
            DirectXList.Text = _DirectXSystemList.Item(0)
            OuvertureRecorder()
        End If
    End Sub

    Private HauteurRecorder As Double
    Private Sub OuvertureRecorder()
        While AnimationEnCours
            Thread.Sleep(100)
        End While
        AnimationEnCours = True
        Visibility = Visibility.Visible
        Dim BordureRecorder = CType(Owner, MainWindow).BordureRecorder
        Dim ValeurDecalage As Double = 198
        HauteurRecorder = ValeurDecalage
        If BordureRecorder IsNot Nothing Then
            Me.Left = Me.PointToScreen(BordureRecorder.TranslatePoint(New Point(0, 0), Me)).X
            Dim AnimationPosition As DoubleAnimation = New DoubleAnimation()
            AnimationPosition.From = Me.PointToScreen(BordureRecorder.TranslatePoint(New Point(0, 0), Me)).Y
            AnimationPosition.To = Me.PointToScreen(BordureRecorder.TranslatePoint(New Point(0, 0), Me)).Y - ValeurDecalage
            AnimationPosition.Duration = New Duration(New TimeSpan(0, 0, 2))
            AnimationPosition.FillBehavior = FillBehavior.Stop
            Me.BeginAnimation(WindowPlayer.TopProperty, AnimationPosition)
            Dim AnimationHauteur As DoubleAnimation = New DoubleAnimation()
            AnimationHauteur.From = BordureRecorder.ActualHeight - 2
            AnimationHauteur.To = HauteurRecorder
            AnimationHauteur.Duration = New Duration(New TimeSpan(0, 0, 2))
            AnimationHauteur.FillBehavior = FillBehavior.Stop
            AddHandler AnimationHauteur.Completed, AddressOf Event_OuvertureRecorderTerminee
            Me.BeginAnimation(WindowPlayer.HeightProperty, AnimationHauteur)
        End If
    End Sub
    Private Sub Event_OuvertureRecorderTerminee(ByVal sender As Object, ByVal e As EventArgs)
        Dim BordureRecorder = CType(Owner, MainWindow).BordureRecorder
        Me.Height = HauteurRecorder
        BordureRecorder.Height = Me.Height
        Me.Top = Me.PointToScreen(BordureRecorder.TranslatePoint(New Point(0, 0), Me)).Y - HauteurRecorder + BordureRecorder.ActualHeight
        AnimationEnCours = False
    End Sub

    Private Sub BPCloseRecorder_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles BPCloseRecorder.Click
        FermetureRecorder()
    End Sub
    Private Function FermetureRecorder() As Boolean
        While AnimationEnCours
            Thread.Sleep(100)
        End While
        AnimationEnCours = True
        Visibility = Visibility.Visible
        Dim BordureRecorder = CType(Owner, MainWindow).BordureRecorder
        Dim ValeurDecalage As Double = 198
        HauteurRecorder = 0
        If BordureRecorder IsNot Nothing Then
            Me.Left = Me.PointToScreen(BordureRecorder.TranslatePoint(New Point(0, 0), Me)).X
            Dim AnimationPosition As DoubleAnimation = New DoubleAnimation()
            AnimationPosition.From = Me.PointToScreen(BordureRecorder.TranslatePoint(New Point(0, 0), Me)).Y
            AnimationPosition.To = Me.PointToScreen(BordureRecorder.TranslatePoint(New Point(0, 0), Me)).Y + ValeurDecalage
            AnimationPosition.Duration = New Duration(New TimeSpan(0, 0, 2))
            AnimationPosition.FillBehavior = FillBehavior.Stop
            Me.BeginAnimation(WindowPlayer.TopProperty, AnimationPosition)
            Dim AnimationHauteur As DoubleAnimation = New DoubleAnimation()
            AnimationHauteur.From = ActualHeight - 2
            AnimationHauteur.To = HauteurRecorder
            AnimationHauteur.Duration = New Duration(New TimeSpan(0, 0, 2))
            AnimationHauteur.FillBehavior = FillBehavior.Stop
            AddHandler AnimationHauteur.Completed, AddressOf Event_FermetureRecorderTerminee
            Me.BeginAnimation(WindowPlayer.HeightProperty, AnimationHauteur)
        End If
        Return True
    End Function
    Private Sub Event_FermetureRecorderTerminee(ByVal sender As Object, ByVal e As EventArgs)
        Dim BordureRecorder = CType(Owner, MainWindow).BordureRecorder
        Me.Height = HauteurRecorder
        BordureRecorder.Height = Me.Height
        AnimationEnCours = False
        If ActualHeight < 200 Then
            HauteurRecorder = 0
            Visibility = Visibility.Visible
        End If
        Close()
    End Sub

    Private Sub DirectXList_PreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles DirectXList.PreviewMouseLeftButtonDown
        If DeviceOccupe Then
            wpfMsgBox.MsgBoxInfo("Changement de système impossible", "Un système est actuellement en cours d'utilisation")
            e.Handled = True
        End If
    End Sub
    Private Sub DirectXList_SelectionChanged(ByVal sender As Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles DirectXList.SelectionChanged
        If IsOk Then
            Try
                Dim NumDevice As Integer = CInt(ExtraitChaine(e.AddedItems(0), "<", ">"))
                If (NumDevice >= 0) And Not DeviceOccupe Then
                    CloseFichier()
                    BASS_RecordFree()
                    Dim Retour = BASS_RecordInit(NumDevice)
                    If Retour = 0 Then Throw New Exception(BASS.BASS_ErrorGetMessage("BASS_Init"))
                    Dim RecordInfos As BASS_RECORDINFO = New BASS_RECORDINFO
                    Retour = BASS_RecordGetInfo(RecordInfos)
                    If Retour = 0 Then Throw New Exception(BASS.BASS_ErrorGetMessage("BASS_Init"))
                    DeviceEnCours = BASS_RecordGetDevice()
                    Dim VolumeEncours As Single
                    Retour = BASS_RecordGetInput(0, VolumeEncours)
                    If Retour = -1 Then Throw New Exception(BASS.BASS_ErrorGetMessage("BASS_Init"))
                    InputType = BASS_GetTypeRecordInput(Retour)
                    RecorderVolume.Value = VolumeEncours * 100
                    Retour = BASS_RecordStart(44100, 2, 0, vRecordProc, 0)
                    If Retour <> 0 Then hRecord = Retour Else Throw New Exception(BASS.BASS_ErrorGetMessage("BASS_Init"))
                    If Not RafraicheAffichage.Enabled Then RafraicheAffichage.Start()
                End If
            Catch ex As Exception
                wpfMsgBox.MsgBoxInfo("Erreur initialisation Recorder", ex.Message)
            End Try
        End If
    End Sub
    Private Sub RecorderVolume_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles RecorderVolume.ValueChanged
        BASS_RecordSetInput(0, Enum_Bass_RecordSetInput.BASS_INPUT_ON, e.NewValue / 100)
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
        CloseFichier()
    End Sub

    'PROCEDURE DE FERMETURE DU LECTEUR APPELE PAR LA FENETRE MAITRE
    Function CloseRecorder() As Boolean
    End Function

    '**************************************************************************************************************
    '**************************************GESTION DU DRAG AND DROP************************************************
    '**************************************************************************************************************

    '***********************************************************************************************
    '---------------------------------FONCTIONS PUBLIQUES DE LA CLASSE------------------------------
    '***********************************************************************************************


    '***********************************************************************************************
    '---------------------------------GESTION SOUND------------------------------------------------
    '***********************************************************************************************
    Private Fichier As fileWave
    Private GaucheValue As Integer
    Private DroiteValue As Integer
    Delegate Sub TraitementMsgMixer()
    Private DureeTotale As Double
    Private Sub RafraicheAffichage_Elapsed(sender As Object, e As Timers.ElapsedEventArgs) Handles RafraicheAffichage.Elapsed
        If IsOk Then
            Dim NewGauche As Integer = GaucheValue
            Dim NewDroite As Integer = DroiteValue
            GetLevel(NewGauche, NewDroite)
            GaucheValue = GaucheValue * 0.96
            DroiteValue = DroiteValue * 0.96
            GaucheValue = Math.Max(GaucheValue, NewGauche)
            DroiteValue = Math.Max(DroiteValue, NewDroite)
            Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                                       New TraitementMsgMixer(Sub()
                                                                                  Gauche.Value = (GaucheValue)
                                                                                  Droite.Value = (DroiteValue)
                                                                                  Dim Minutes As Integer = Int(WaveForm.PositionPlay / 60)
                                                                                  Dim Secondes As Integer = Int((WaveForm.PositionPlay) - Minutes * 60)
                                                                                  Dim Centieme As Integer = (WaveForm.PositionPlay - Minutes * 60 - Secondes) * 99
                                                                                  TempsAfficheurPlay.Text = Minutes.ToString("00") & " : " & Secondes.ToString("00") & " : " & Centieme.ToString("00")
                                                                              End Sub))
        End If
    End Sub
    Private Sub GetLevel(ByRef Gauche As Integer, ByRef droite As Integer)
        If hRecord <> 0 Then
            Dim level As UInt32
            level = BASS_ChannelGetLevel(hRecord)
            If level > 0 Then
                Dim newGauche As Integer = LoWord(level)
                Dim newDroite As Integer = HiWord(level)
                If newGauche < &HFFFF Then Gauche = newGauche
                If newDroite < &HFFFF Then droite = newDroite
            End If
        End If
    End Sub
    Private Function RECORDPROCInterne(ByVal handle As IntPtr, ByVal buffer As IntPtr, ByVal length As Int32, user As IntPtr) As Int32
        If Fichier IsNot Nothing Then
            Dim TabBytes(length - 1) As Byte
            Dim TabInt16(length / 2 - 1) As Int16
            Marshal.Copy(buffer, TabBytes, 0, length)
            Fichier.WriteData(TabBytes)
            Marshal.Copy(buffer, TabInt16, 0, length / 2)
            DureeTotale += length / (44100 * 4)
            Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                                        New TraitementMsgMixer(Sub()
                                                                                   WaveForm.AddData(DureeTotale, TabInt16)
                                                                                   Dim Minutes As Integer = Int(WaveForm.DureeTotale / 60)
                                                                                   Dim Secondes As Integer = Int((WaveForm.DureeTotale) - Minutes * 60)
                                                                                   Dim Centieme As Integer = (WaveForm.DureeTotale - Minutes * 60 - Secondes) * 99
                                                                                   TempsAfficheurRec.Text = Minutes.ToString("00") & " : " & Secondes.ToString("00") & " : " & Centieme.ToString("00")
                                                                               End Sub))
        End If
        Return True
    End Function
    Private Function CreateFile(FileName As String) As Boolean
        If Fichier Is Nothing Then
            Fichier = New fileWave()
            Dim DateDuJour As String = DateAndTime.Day(Now) & "_" & Month(Now) & "_" & Year(Now) & "_" & Hour(Now) & "-" & Minute(Now) & "-" & Second(Now)
            If Fichier.CreateFile(RepDest & "test " & DateDuJour & ".wav") Then
                WaveForm.InitAffichage()
                Etat = EtatRecorder.Record
                DureeTotale = 0
                Return True
            Else
                Fichier = Nothing
            End If
        End If
        Return False
    End Function
    Private Sub CloseFichier()
        If Fichier IsNot Nothing Then
            Dim FileHeader As WAVEFILEHEADER = New WAVEFILEHEADER
            FileHeader.NbrCanaux = 2
            FileHeader.Frequence = 44100
            FileHeader.NbrOctetsSeconde = 44100 * 4
            FileHeader.NbrOctetsEchant = 4
            FileHeader.NbrBitsEchant = 16
            Fichier.WriteFileHeader(FileHeader)
            Fichier.CloseFile()
            Fichier = Nothing
        End If
        BPDebut.IsEnabled = True
        BPArriere.IsEnabled = True
        BPRecord.IsEnabled = True
        BPPlay.IsEnabled = True
        BPPause.IsEnabled = True
        BPStop.IsEnabled = True
        BPAvance.IsEnabled = True
        BPFin.IsEnabled = True
        Etat = EtatRecorder.Arret
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION INTERFACE---------------------------------------------
    '***********************************************************************************************
    Private Sub BPDebut_Click(sender As Object, e As RoutedEventArgs) Handles BPDebut.Click
        WaveForm.AffichageSegment(0)
    End Sub
    Private Sub BPDebut_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs) Handles BPDebut.PreviewMouseDown

    End Sub
    Private Sub BPArriere_Click(sender As Object, e As RoutedEventArgs) Handles BPArriere.Click
        WaveForm.AffichageSegment(WaveForm.SegmentAffiche - 1)
    End Sub
    Private Sub BPArriere_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs) Handles BPArriere.PreviewMouseDown
    End Sub
    Private Sub BPRecord_Click(sender As Object, e As RoutedEventArgs) Handles BPRecord.Click
        If CreateFile(RepDest & "\" & "test.wav") Then
            BPDebut.IsEnabled = False
            BPArriere.IsEnabled = False
            BPRecord.IsEnabled = False
            BPPlay.IsEnabled = False
            BPPause.IsEnabled = False
            BPStop.IsEnabled = True
            BPAvance.IsEnabled = False
            BPFin.IsEnabled = False
            imBpRecord.Source = GetBitmapImage("../Images/imgboutons/recordtravail24.png")
        End If
    End Sub

    Private WithEvents SimulPlay As Timers.Timer
    Dim positionplay As Double
    Private Sub SimulPlay_Elapsed(sender As Object, e As Timers.ElapsedEventArgs) Handles SimulPlay.Elapsed
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                                    New TraitementMsgMixer(Sub()
                                                                               If WaveForm.AffichagePlay(positionplay) Then
                                                                                   positionplay += 0.02 * 44100 * 4
                                                                               Else
                                                                                   SimulPlay.Stop()
                                                                               End If
                                                                           End Sub))
    End Sub
    Private Sub BPPlay_Click(sender As Object, e As RoutedEventArgs) Handles BPPlay.Click
        positionplay = 0
        SimulPlay = New Timers.Timer
        SimulPlay.Interval = 20
        SimulPlay.Start()

        BPDebut.IsEnabled = False
        BPArriere.IsEnabled = False
        BPRecord.IsEnabled = False
        BPPlay.IsEnabled = False
        BPPause.IsEnabled = True
        BPStop.IsEnabled = True
        BPAvance.IsEnabled = False
        BPFin.IsEnabled = False
        Etat = EtatRecorder.Play
        imBpPlay.Source = GetBitmapImage("../Images/imgboutons/playtravail24.png")
        imBpPause.Source = GetBitmapImage("../Images/imgboutons/pause24.png")
    End Sub
    Private Sub BPPause_Click(sender As Object, e As RoutedEventArgs) Handles BPPause.Click
        If Etat = EtatRecorder.Play Then
            BPPlay.IsEnabled = True
            BPPause.IsEnabled = True
            BPStop.IsEnabled = True
            imBpPause.Source = GetBitmapImage("../Images/imgboutons/pausetravail24.png")
            imBpPlay.Source = GetBitmapImage("../Images/imgboutons/play24.png")
            Etat = EtatRecorder.Pause
        End If
    End Sub
    Private Sub BPStop_Click(sender As Object, e As RoutedEventArgs) Handles BPStop.Click
        If SimulPlay IsNot Nothing Then SimulPlay.Stop()
        CloseFichier()
        imBpRecord.Source = GetBitmapImage("../Images/imgboutons/record24.png")
        imBpPlay.Source = GetBitmapImage("../Images/imgboutons/play24.png")
        imBpPause.Source = GetBitmapImage("../Images/imgboutons/pause24.png")
    End Sub

    Private Sub BPAvance_Click(sender As Object, e As RoutedEventArgs) Handles BPAvance.Click
        'WaveForm.AffichageSegment(WaveForm.SegmentAffiche + 1)
        WaveForm.Visibility = Windows.Visibility.Visible

    End Sub
    Private Sub BPFin_Click(sender As Object, e As RoutedEventArgs) Handles BPFin.Click
        'WaveForm.AffichageSegment(WaveForm.SegmentsTotal)
        Dim FichierZoom As fileWave
        Dim PositionDebut As Double = WaveForm.PositionPlay
        Dim DureeData As Double
        FichierZoom = New fileWave()
        If FichierZoom.OpenFile(RepDest & "\" & "test.wav") Then
            WaveForm.Visibility = Windows.Visibility.Hidden
            Zoom.DureeSegment = 0.2
            DureeData = Zoom.DureeSegment * 1.2
            Zoom.InitAffichage()
            FichierZoom.SetNextPlayPosition(PositionDebut * 44100 * 4)
            Dim TabBytes(DureeData * 44100 * 4 - 1) As Byte
            TabBytes = FichierZoom.ReadWaveData(DureeData * 44100 * 4)
            Dim TabInt16(TabBytes.Length / 2) As Int16
            Dim Mem As IntPtr = Marshal.AllocHGlobal(TabBytes.Length)
            Marshal.Copy(TabBytes, 0, Mem, TabBytes.Length)
            Marshal.Copy(Mem, TabInt16, 0, TabBytes.Length / 2)
            Marshal.FreeHGlobal(Mem)
            'TabBytes.CopyTo(TabInt16, 0)
            Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                                        New TraitementMsgMixer(Sub()
                                                                                   Zoom.AddData(DureeData, TabInt16, True)
                                                                                   Zoom.AffichageSegment(0)
                                                                                   FichierZoom.CloseFile()
                                                                                   FichierZoom = Nothing
                                                                               End Sub))
            ' Etat = EtatRecorder.Record
            ' DureeTotale = 0
            'Return True
        End If
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


