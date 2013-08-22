Option Compare Text
'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 26/12/11
'DESCRIPTION :Classe fichier wave
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.IO
Imports System.Threading

Public Class fileCdAudio
    Inherits fileAudio
    '***********************************************************************************************
    '-------------------------------STRUCTURES PUBLIQUES--------------------------------------------
    '***********************************************************************************************

    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Private Position As Long              'Position de lecture des données dans le fichier
    Private TailleFichierWave As Long     'Taille du fichier wave
    Private NumDrive As Integer
    Private NumTrack As Integer
    Private StreamCD As BassStream
    Private NomFichier As String
    Private FileForConvert As Boolean

    '***********************************************************************************************
    '---------------------------------CONSTRUCTEUR DE LA CLASSE-------------------------------------
    '***********************************************************************************************
     '***********************************************************************************************
    '----------------------------------DESTRUCTEUR DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Protected Overrides Sub Finalize()
        If FichierAudio IsNot Nothing Then FichierAudio.CloseFile()
        MyBase.Finalize()
    End Sub
    '***********************************************************************************************
    '----------------------------PROPRIETES DE LA CLASSE EN LECTURE/ECRITURE------------------------
    '***********************************************************************************************
    '***********************************************************************************************
    '----------------------------LECTURE DES PROPRIETES DE LA CLASSE--------------------------------
    '***********************************************************************************************
    '--------------------------Retourne le nombre de canaux------------------------------
    Public Overrides ReadOnly Property WaveNbrDeCanaux() As Int16
        Get
            Return 2
        End Get
    End Property
    '--------------------------Retourne la frequence -------------------------------------
    Public Overrides ReadOnly Property WaveFrequence() As Int32
        Get
            Return 44100
        End Get
    End Property
    '--------------------------Retourne le taux de compression---------------------------
    Public Overrides ReadOnly Property WaveTauxCompression() As String
        Get
            Return "14H"
        End Get
    End Property
    '--------------------------Retourne le nombre d'octets par seconde -------------------
    Public Overrides ReadOnly Property WaveOctetParSec() As Int32
        Get
            Return 44100 * 4
        End Get
    End Property
    '--------------------------Retourne le nombre d'octets par échantillon ---------------
    Public Overrides ReadOnly Property WaveOctetParEchant() As Int16
        Get
            Return 4
        End Get
    End Property
    '--------------------------Retourne le nombre de bits par echantillon ----------------
    Public Overrides ReadOnly Property WaveBitsParEchant() As Int16
        Get
            Return 16
        End Get
    End Property
    '----------------------Retourne la duree totale du fichier--------------------------------------
    Public Overrides ReadOnly Property PlayDureeTotale() As Integer
        Get
            Return TailleFichierWave
        End Get
    End Property
    '----------------------Lecture du nom du fichier------------------------------------------------
    Public Overrides ReadOnly Property FileName() As String
        Get
            Return NomFichier
        End Get
    End Property
    '--------------------------Retourne le nombre d'octets du fichier-------------------------------
    Public Overrides ReadOnly Property FileSize() As Integer
        Get
            If StreamCD IsNot Nothing Then Return StreamCD.ChannelLenght Else Return 0
        End Get
    End Property
    '----------------------Lecture du nom du fichier------------------------------------------------
    Public Overrides ReadOnly Property FilePosition As Long
        Get
            If StreamCD IsNot Nothing Then Return StreamCD.ChannelPosition Else Return 0
        End Get
    End Property

    '***********************************************************************************************
    '-------------------------------METHODES PUBLIQUES DE LA CLASSE---------------------------------
    '***********************************************************************************************
    '----------------------Fixe la position de lecture à venir--------------------------------------
    Public Overrides Function SetNextPlayPosition(ByVal Pos As Long) As Long
        Dim NewPosition As Long
        NewPosition = Pos
        If NewPosition / CLng(2) <> CLng(NewPosition / CLng(2)) Then NewPosition = NewPosition + 1
        Position = NewPosition
        StreamCD.ChannelPosition = Position
        '  Call FichierAudio.ChangePointer(Position)
        Return NewPosition
    End Function
    '----------------------Fonction d'ouverture du fichier CD AUDIO-------------------------------------
    Public Overrides Function OpenFile(ByVal Name As String, Optional ByVal Forcage As Boolean = False) As Boolean
        If Path.GetExtension(Name) = ".CD" Then
            Position = 0
            NumDrive = CInt(ExtraitChaine(Name, "[", "]"))
            NumTrack = CInt(ExtraitChaine(Name, "-", "."))
            NomFichier = Name
            Try
                BASS_SetConfig(Enum_Bass_SetConfig.BASS_CONFIG_CD_FREEOLD, 0)
                StreamCD = New BassStream(NumDrive, NumTrack, Enum_Bass_Stream.BASS_STREAM_DECODE, FileForConvert)
                TailleFichierWave = StreamCD.ChannelLenght
            Catch ex As Exception
                Return False
            End Try
            Return True
        End If
        Return False
    End Function
    Public Function OpenFileForConvert(ByVal Name As String) As Boolean
        FileForConvert = True
        Dim retour = OpenFile(Name)
        If retour = False Then
            While BassCDDrive.DriveIsBusy(NumDrive)
                Thread.Sleep(200)
            End While
            Return OpenFile(Name)
        Else
            Return True
        End If
    End Function
    '----------------------Fonction de fermeture du fichier WAVE------------------------------------
    Public Overrides Function CloseFile() As Boolean
        TailleFichierWave = 0
        If StreamCD IsNot Nothing Then StreamCD.Close()
        Return True
    End Function
    '----------------------Fonction de lecture des données--------------------------------------
    Private TailleBufferWave As Integer        'Taille du buffer données au format WAVE
    Public Overrides Function ReadWaveData(ByVal TailleBuffer As Long) As Byte()
        If TailleBufferTampon = 0 Then InitDataBuffer(0)
        Return ReadDataBuffer(TailleBuffer)
    End Function
    '***********************************************************************************************
    '----------------------TRAITEMENT DE LA MEMOIRE TAMPON-------------------
    '***********************************************************************************************
    Private TailleBufferTampon As Long
    Private DernierePosEcriture As Long
    Private BufferTampon() As Byte '
    Private Sub InitDataBuffer(ByVal NewPosition As Long)
        TailleBufferTampon = PlayBufferSize() * 4
        Dim Buffer(TailleBufferTampon - 1) As Byte
        BufferTampon = Buffer
        DernierePosEcriture = 0
    End Sub
    Private Function ReadDataBuffer(ByVal TailleBuffer As Long) As Byte()
        Do Until DernierePosEcriture > PlayBufferSize * 2
            Dim TailleBufferLecture As Long = 2352  'ailleBufferWave
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
        Position = StreamCD.ChannelPosition
        If Position = TailleFichierWave Then Return Nothing
        If Position + TailleBuffer > TailleFichierWave Then
            TailleBuffer = TailleFichierWave - Position
        End If
        Dim Buffer() As Byte = StreamCD.ReadData(TailleBuffer)
        If Buffer IsNot Nothing Then
            Position = Position + Buffer.Length 'TailleBufferMp3
            Return Buffer
        End If
    End Function

    '----------------------Fonction de création d'un fichier WAVE-----------------------------------
    Public Overrides Function CreateFile(ByVal Name As String) As Boolean
        Return False
    End Function
    '----------------------Fonction d'écriture des données-----------------------------------
    Public Overrides Function WriteData(ByVal Buffer() As Byte) As Boolean
        Return False
    End Function
    '***********************************************************************************************
    '-------------------------------METHODES PRIVEES DE LA CLASSE---------------------------------
    '***********************************************************************************************
End Class
