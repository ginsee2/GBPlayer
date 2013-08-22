Option Compare Text
'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 26/12/11
'DESCRIPTION :Classe fichier mp3
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports Microsoft.DirectX
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.IO

Public Class fileMp3
    Inherits fileAudio
    '***********************************************************************************************
    '-------------------------------STRUCTURES PUBLIQUES--------------------------------------------
    '***********************************************************************************************
    Public Structure MPEGLAYER3FRAMEHEADER
        Public PositionTrames As Integer       'Stocke la position de l'entete dans le fichier
        Public Format As Integer               'Type format MPEGxLAYER
        Public FlagNoCRCProtected As Boolean    'false : Protection true:Pas de protection
        Public Bitrate As Integer              'Bitrate
        Public Frequence As Integer            'Frequence d'échantillonnage
        Public FlagIsPaddingUsed As Boolean     'Le padding est utilisé
        Public FlagPrivate As Boolean           'Flag private actif
        Public Channel As MP3_CHANNEL_MODE              'Type BE_MP3_MODE_xxxx
        Public FlagCopyright As Boolean         'Flag copyright actif
        Public FlagOriginal As Boolean          'Flag original actif
        Public FlagEmphasis As Boolean          'Infos emphasis
        Public FlagUseVBR As Boolean            'Flag VBR actif
        Public FlagFramesNumber As Boolean  'TRUE Si VBR Nombre de trames du fichier mp3 valide
        Public FlagFileSize As Boolean      'TRUE si VBR Taille du fichier valide
        Public FramesNumber As Integer         'Nombre de frames du fichier mp3
        Public FileDataSize As Integer         'Taille du fichier hors TAG
        Public TimeInSeconde As Integer        'Durée du morceau en seconde
        Public DureeFrame As Single         'Durée d'une frame
        Public IDv1TAGPresent As Boolean
        Public IDv2TAGPresent As Boolean
    End Structure

    '***Constantes nMode de la structure BE_CONFIG
    Public Enum MP3_CHANNEL_MODE
        Stereo = 0
        JointStereo = 1
        DualChannel = 2
        Mono = 3
    End Enum
    '---Constantes utilisées dans la structure MPEGLAYER3FRAMEHEADER
    Public Enum MP3_FORMAT
        MPEG1LAYER3 = 1
        MPEG2LAYER3 = 2
        MPEG25LAYER3 = -2
    End Enum

    Private lFrameHeader As MPEGLAYER3FRAMEHEADER 'Entete du fichier mp3
    Private TailleDataMp3 As Long           'Taille des données du fichier mp3
    Private Position As Long                'Position de lecture des données dans le fichier
    Private TailleBufferMp3 As Integer         'Taille du buffer données au format mp3
    Private TailleBufferWave As Integer        'Taille du buffer données au format WAVE
    Private Converter As fileDecoderMp3       'Classe de conversion

    '--------------------------Retourne le nombre de canaux------------------------------
    Public Overrides ReadOnly Property WaveNbrDeCanaux As Int16
        Get
            If lFrameHeader.Channel = MP3_CHANNEL_MODE.Mono Then Return 1 Else Return 2
        End Get
    End Property
    '--------------------------Retourne la frequence -------------------------------------
    Public Overrides ReadOnly Property WaveFrequence As Int32
        Get
            Return lFrameHeader.Frequence
        End Get
    End Property
    '--------------------------Retourne le taux de compression---------------------------
    Public Overrides ReadOnly Property WaveTauxCompression As String
        Get
            Return CStr(lFrameHeader.Bitrate)
        End Get
    End Property
    '--------------------------Retourne le nombre d'octets par seconde -------------------
    Public Overrides ReadOnly Property WaveOctetParSec As Int32
        Get
            Return WaveFrequence * WaveNbrDeCanaux * 2
        End Get
    End Property
    '--------------------------Retourne le nombre d'octets par échantillon ---------------
    Public Overrides ReadOnly Property WaveOctetParEchant As Int16
        Get
            Return WaveNbrDeCanaux * 2
        End Get
    End Property
    '--------------------------Retourne le nombre de bits par echantillon ----------------
    Public Overrides ReadOnly Property WaveBitsParEchant As Int16
        Get
            Return 16
        End Get
    End Property
    '----------------------Retourne la duree totale du fichier--------------------------------------
    Public Overrides ReadOnly Property PlayDureeTotale As Integer
        Get
            Return CLng(lFrameHeader.TimeInSeconde) * WaveOctetParSec
        End Get
    End Property
    '----------------------Retourne si le fichier est en lecture force------------------------------
    Public Property ForcageLecture() As Boolean = False
    '----------------------Retourne la structure d'entete du fichier mp3----------------------------
    Public ReadOnly Property GetMp3FrameHeader As MPEGLAYER3FRAMEHEADER
        Get
            Return lFrameHeader
        End Get
    End Property
    '----------------------Retourne la taille d'une frame------------------------------------
    Public ReadOnly Property Mp3FrameSize() As Short
        Get
            Return (((144000 / Math.Abs(lFrameHeader.Format)) * lFrameHeader.Bitrate) / lFrameHeader.Frequence)
        End Get
    End Property
    '***********************************************************************************************
    '-------------------------------METHODES PUBLIQUES DE LA CLASSE---------------------------------
    '***********************************************************************************************
    '----------------------Fixe la position de lecture à venir--------------------------------------
    Public Overrides Function SetNextPlayPosition(ByVal Pos As Long) As Long
        Dim NewPosition As Long
        NewPosition = TailleDataMp3 * (Pos / PlayDureeTotale)
        Call FichierAudio.ChangePointer(NewPosition)
        Position = NewPosition
        Return Pos
    End Function
    '----------------------Fonction d'ouverture du fichier MP3-------------------------------------
    Public Overrides Function OpenFile(ByVal Name As String, Optional ByVal Forcage As Boolean = False) As Boolean
        Dim Access As Integer = FileAccess.ReadWrite
        Dim Share As Integer = FileShare.None
        If Path.GetExtension(Name) = ".MP3" Then
            ForcageLecture = Forcage
            Dim Infos As FileInfo = New FileInfo(Name)
            If (Infos.Attributes And FileAttributes.ReadOnly) = FileAttributes.ReadOnly Then ForcageLecture = True
            If ForcageLecture Then
                Access = FileAccess.Read
                Share = FileShare.Read
            End If
            If FichierAudio.OpenFile(Name, Access, Share) Then
                Position = 0
                If Not ReadFileHeader() Then
                    CloseFile()
                    Return False
                End If
                TailleDataMp3 = FichierAudio.FileSize() - FichierAudio.PositionPointer()
                Return True
            End If
        End If
    End Function
    '----------------------Fonction de fermeture du fichier MP3------------------------------------
    Public Overrides Function CloseFile() As Boolean
        If Not (Converter Is Nothing) Then
            Call Converter.CloseDecoderMp3()
            Converter = Nothing
        End If
        Position = 0
        Return FichierAudio.CloseFile
    End Function
    '----------------------Fonction de création d'un fichier WAVE-----------------------------------
    Public Overrides Function CreateFile(ByVal Name As String) As Boolean
        Debug.Print("Creation : " & Name)
        If FileIsOpen Then CloseFile()
        If FichierAudio.CreateNewFile(Name) Then 'System.IO.Path.ChangeExtension(Name, ".mp3")) Then
            Return True
        End If
        Return False
    End Function
    '----------------------Fonction appelée par le Player-------------------------------------------
    Public Overrides Function ReadWaveData(ByVal TailleBuffer As Long) As Byte()
        If Converter Is Nothing Then
            Converter = New fileDecoderMp3
            TailleBufferWave = Converter.OpenDecoderMp3(Me)
            InitDataBuffer(0)
            If TailleBufferWave = 0 Then
                CloseFile()
                Return Nothing
            End If
        End If
        Return ReadDataBuffer(TailleBuffer)
    End Function
    '***********************************************************************************************
    '----------------------TRAITEMENT DE LA MEMOIRE TAMPON-------------------
    '***********************************************************************************************
    Private TailleBufferTampon As Long
    Private DernierePosEcriture As Long
    Private BufferTampon() As Byte
    Private Sub InitDataBuffer(ByVal NewPosition As Long)
        TailleBufferTampon = PlayBufferSize() * 3
        Dim Buffer(TailleBufferTampon - 1) As Byte
        BufferTampon = Buffer
        DernierePosEcriture = 0
    End Sub
    Private Function ReadDataBuffer(ByVal TailleBuffer As Long) As Byte()
        Do Until DernierePosEcriture > PlayBufferSize
            Dim TailleBufferLecture As Long = TailleBufferWave
            Dim BufferRead() As Byte = ReadNewData(TailleBufferLecture)
            If BufferRead Is Nothing Then Exit Do
            Array.Copy(BufferRead, 0, BufferTampon, DernierePosEcriture, BufferRead.Length)
            DernierePosEcriture = DernierePosEcriture + BufferRead.Length
        Loop
        If DernierePosEcriture > 0 Then
            Dim TailleBufferReturn = Math.Min(TailleBuffer, DernierePosEcriture)
            Dim BufferReturn(TailleBufferReturn - 1) As Byte
            Array.Copy(BufferTampon, 0, BufferReturn, 0, TailleBufferReturn)
            If TailleBuffer < DernierePosEcriture Then
                Dim NewBufferEntree(TailleBufferTampon - 1) As Byte
                Array.Copy(BufferTampon, TailleBuffer, NewBufferEntree, 0, DernierePosEcriture - TailleBuffer)
                BufferTampon = NewBufferEntree
                DernierePosEcriture = DernierePosEcriture - TailleBuffer
            Else
                DernierePosEcriture = 0
            End If
            Return BufferReturn
        Else
            Return Nothing
        End If
    End Function
    Private Function ReadNewData(ByRef TailleBuffer As Long) As Byte()
        TailleBufferMp3 = Mp3FrameSize()
        Position = FichierAudio.PositionPointer()
        If Position >= FileSize() Then Return Nothing
        If Position + TailleBufferMp3 > FileSize() Then
            TailleBufferMp3 = FileSize() - Position
        End If
        Dim BufferMp3() As Byte = FichierAudio.ReadData(TailleBufferMp3)
        TailleBuffer = TailleBufferWave
        Dim Buffer(TailleBuffer - 1) As Byte
        Buffer = Converter.ConvertBufferMp3ToWav(BufferMp3) '
        If Buffer IsNot Nothing Then
            ' Then ', Buffer, TailleBuffer) Then
            Position = Position + Buffer.Length 'TailleBufferMp3
            Return Buffer
        End If
    End Function
    '----------------------Fonction d'écriture des données-----------------------------------
    Public Overrides Function WriteData(ByVal Buffer() As Byte) As Boolean
        If Buffer IsNot Nothing Then
            Return FichierAudio.WriteData(Buffer)
        End If
        Return False
    End Function
    '***********************************************************************************************
    '----------------------TRAITEMENT DES INFORMATIONS DE L'ENTETE DU FICHIER MP3-------------------
    '***********************************************************************************************
    '--------------------------Fonction de lecture des infos VBR dans le fichier mp3----------------
    Private Function ReadFileHeader() As Boolean 'Long
        lFrameHeader = New MPEGLAYER3FRAMEHEADER
        'DETECTION PRESENCE DES INFORMATIONS DE TYPE ID3v1 EN PIED DE FICHIER
        FichierAudio.ChangePointer(-128, SeekOrigin.End)
        If (FichierAudio.ReadString(3) = "TAG") Then lFrameHeader.IDv1TAGPresent = True
        'DETECTION PRESENCE DES INFORMATIONS DE TYPE ID3v2 EN TETE DE FICHIER
        FichierAudio.ChangePointer(0)
        If (FichierAudio.ReadString(3) = "ID3") Then
            'LECTURE PARTIE HEADER
            lFrameHeader.IDv2TAGPresent = True
            FichierAudio.ChangePointer(6)
            Dim TagSize = TagID3.tagID3Object.FonctionUtilite.DecodeSynchSafe(FichierAudio.ReadData(4))
            FichierAudio.ChangePointer(TagSize + 10)
        Else
            lFrameHeader.IDv2TAGPresent = False
            FichierAudio.ChangePointer(0)
        End If
        'DETECTION FICHIER MP3 LECTURE DE L'ENTETE DE LA PREMIERE TRAME
        Do Until FichierAudio.PositionPointer = FichierAudio.FileSize
            If ((FichierAudio.ReadByte() And 255) = 255) Then
                If ((FichierAudio.ReadByte() And 224) = 224) Then
                    Call FichierAudio.ChangePointer(-1, SeekOrigin.Current)
                    Dim MemPos As Long = FichierAudio.PositionPointer
                    If ReadFileHeaderInfo() Then
                        Exit Do
                    Else
                        FichierAudio.ChangePointer(MemPos)
                    End If
                End If
            End If
        Loop
        Return True
    End Function
    Private Function ReadFileHeaderInfo() As Boolean 'Long
        Dim LectureTexte As String
        Dim Lecture As Byte
        Dim PositionDebutDonnees As Long
        Dim TableauBitrate(), TableauFrequence(), TableauChannelMode() As Integer
        Try
            lFrameHeader.PositionTrames = FichierAudio.PositionPointer - 1
            PositionDebutDonnees = lFrameHeader.PositionTrames
            Lecture = FichierAudio.ReadByte
            Select Case (Lecture And &H1A)
                Case &H1A
                    lFrameHeader.Format = MP3_FORMAT.MPEG1LAYER3
                    TableauBitrate = {0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320}
                    TableauFrequence = {44100, 48000, 32000}
                    lFrameHeader.DureeFrame = 0.026
                Case &H12
                    lFrameHeader.Format = MP3_FORMAT.MPEG2LAYER3
                    TableauBitrate = {0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160}
                    TableauFrequence = {22050, 24000, 16000}
                    lFrameHeader.DureeFrame = 0.036
                Case &H2
                    lFrameHeader.Format = MP3_FORMAT.MPEG25LAYER3
                    TableauBitrate = {0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160}
                    TableauFrequence = {11025, 12000, 8000}
                    lFrameHeader.DureeFrame = 0.052
                Case Else
                    If ForcageLecture Then
                        lFrameHeader.Format = MP3_FORMAT.MPEG1LAYER3
                        TableauBitrate = {0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320}
                        TableauFrequence = {44100, 48000, 32000}
                        lFrameHeader.DureeFrame = 0.026
                    Else
                        Throw New Exception("Erreur de bitrate dans le fichier mp3")
                    End If
            End Select
            lFrameHeader.FlagNoCRCProtected = CBool((Lecture And &H1) = &H1)
            Lecture = FichierAudio.ReadByte()
            lFrameHeader.Bitrate = TableauBitrate(((Lecture And &HF0) / 16))
            lFrameHeader.Frequence = TableauFrequence(((Lecture And &HC) / 4))
            'TEMPORAIRE POUR EVITER LES PLANTAGES
            'If lFrameHeader.bBitrate < 128 Then SendErrorMessage(GBAU_ERR_CLSFILEREAD)
            'If lFrameHeader.lFrequence < 32000 Then SendErrorMessage(GBAU_ERR_CLSFILEREAD)
            lFrameHeader.FlagIsPaddingUsed = CBool((Lecture And &H2) = &H2)
            lFrameHeader.FlagPrivate = CBool((Lecture And &H1) = &H1)
            Lecture = FichierAudio.ReadByte()
            TableauChannelMode = {MP3_CHANNEL_MODE.Stereo, MP3_CHANNEL_MODE.JointStereo, MP3_CHANNEL_MODE.DualChannel, MP3_CHANNEL_MODE.Mono}
            lFrameHeader.Channel = TableauChannelMode(((Lecture And &HC0) / 64))
            lFrameHeader.FlagCopyright = CBool((Lecture And &H8) = &H8)
            lFrameHeader.FlagOriginal = CBool((Lecture And &H4) = &H4)
            lFrameHeader.FlagEmphasis = CBool((Lecture And &H3) = &H3)
            lFrameHeader.FileDataSize = FichierAudio.FileSize
            If lFrameHeader.IDv1TAGPresent Then lFrameHeader.FileDataSize = lFrameHeader.FileDataSize - 128
            If lFrameHeader.IDv2TAGPresent Then lFrameHeader.FileDataSize = lFrameHeader.FileDataSize - lFrameHeader.PositionTrames
            lFrameHeader.FramesNumber = CLng((lFrameHeader.FileDataSize / ((144000 / Math.Abs(lFrameHeader.Format)) * lFrameHeader.Bitrate)) _
                                            * lFrameHeader.Frequence)
            'DETECTION FICHIER MP3 UTILISANT LE VBR
            FichierAudio.ChangePointer(36 + lFrameHeader.PositionTrames, SeekOrigin.Begin)
            LectureTexte = FichierAudio.ReadString(4)
            If (LectureTexte = "Xing") Or (LectureTexte = "Info") Then
                lFrameHeader.FlagUseVBR = True
                FichierAudio.ChangePointer(3, SeekOrigin.Current)
                Lecture = FichierAudio.ReadByte()
                If (Lecture And &H1) = &H1 Then lFrameHeader.FlagFramesNumber = True
                If (Lecture And &H2) = &H2 Then lFrameHeader.FlagFileSize = True
                If lFrameHeader.FlagFramesNumber Then lFrameHeader.FramesNumber = FichierAudio.ReadDMotInverse
                If lFrameHeader.FlagFileSize Then lFrameHeader.FileDataSize = FichierAudio.ReadDMotInverse
                lFrameHeader.Bitrate = CLng(((lFrameHeader.FileDataSize / lFrameHeader.FramesNumber) _
                                            * lFrameHeader.Frequence) / (144000 / Math.Abs(lFrameHeader.Format)))
            ElseIf LectureTexte = "VBRI" Then
                lFrameHeader.FlagUseVBR = True
                FichierAudio.ChangePointer(6, SeekOrigin.Current)
                lFrameHeader.FileDataSize = FichierAudio.ReadDMotInverse
                lFrameHeader.FramesNumber = FichierAudio.ReadDMotInverse
                lFrameHeader.Bitrate = CLng(((lFrameHeader.FileDataSize / lFrameHeader.FramesNumber) _
                                            * lFrameHeader.Frequence) / (144000 / Math.Abs(lFrameHeader.Format)))
            Else
                lFrameHeader.FlagUseVBR = False
            End If
            'FIN DE LA LECTURE DE L'ENTETE
            lFrameHeader.TimeInSeconde = CLng(lFrameHeader.FramesNumber * lFrameHeader.DureeFrame * (44100 / lFrameHeader.Frequence))
            FichierAudio.ChangePointer(PositionDebutDonnees)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function
End Class
