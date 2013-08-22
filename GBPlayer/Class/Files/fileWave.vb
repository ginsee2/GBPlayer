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
Imports Microsoft.DirectX
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.IO

Public Class fileWave
    Inherits fileAudio

    '***********************************************************************************************
    '-------------------------------STRUCTURES PUBLIQUES--------------------------------------------
    '***********************************************************************************************
    Public Structure WAVEFILEHEADER
        Public IdFichier As String              'Chaine "RIFF" identifiant les fichiers RIFF
        Public TailleFichier As Int32         '4 Taille en octet du restant du fichier
        Public IdFormatWAV As String            '8Chaine "WAVEfmt " identifiant le format WAVE
        Public LgDefFormat As Int32           '16Longueur de la définition du format WAVE
        Public NumFormat As Int16               '20Numéro du format
        Public NbrCanaux As Int16             '22Nombre de canaux
        Public Frequence As Int32             '24Frequence = nbr d'échantillons par seconde
        Public NbrOctetsSeconde As Int32      '28Nbr d'octets par seconde
        Public NbrOctetsEchant As Int16         '32Nbr d'octets par échantillon * nbr canaux
        Public NbrBitsEchant As Int16           '34Nombre de bit par échantillon
        Public IdDonnees As String              '36Chaine "data" identifiant le début des données
        Public LgDonnees As Int32               '40Nombre d'octets de données
    End Structure

    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Private Position As Long              'Position de lecture des données dans le fichier
    Private lFileHeader As WAVEFILEHEADER 'Entete du fichier wave
    Private TailleFichierWave As Long     'Taille du fichier wave

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
            Return lFileHeader.NbrCanaux
        End Get
    End Property
    '--------------------------Retourne la frequence -------------------------------------
    Public Overrides ReadOnly Property WaveFrequence() As Int32
        Get
            Return lFileHeader.Frequence
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
            Return lFileHeader.NbrOctetsSeconde
        End Get
    End Property
    '--------------------------Retourne le nombre d'octets par échantillon ---------------
    Public Overrides ReadOnly Property WaveOctetParEchant() As Int16
        Get
            Return lFileHeader.NbrOctetsEchant
        End Get
    End Property
    '--------------------------Retourne le nombre de bits par echantillon ----------------
    Public Overrides ReadOnly Property WaveBitsParEchant() As Int16
        Get
            Return lFileHeader.NbrBitsEchant
        End Get
    End Property
    '----------------------Retourne la duree totale du fichier--------------------------------------
    Public Overrides ReadOnly Property PlayDureeTotale() As Integer
        Get
            Return TailleFichierWave - Len(lFileHeader)
        End Get
    End Property
    '----------------------Retourne la structure d'entete du fichier--------------------------------------
    Public ReadOnly Property GetWaveFileHeader() As WAVEFILEHEADER
        Get
            Return lFileHeader
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
        Call FichierAudio.ChangePointer(Position)
        Return NewPosition
    End Function
    '----------------------Fonction d'ouverture du fichier WAVE-------------------------------------
    Public Overrides Function OpenFile(ByVal Name As String, Optional ByVal Forcage As Boolean = False) As Boolean
        If Path.GetExtension(Name) = ".WAV" Then
            Position = 0
            If FichierAudio.OpenFile(Name) Then
                If Not ReadFileHeader() Then
                    CloseFile()
                    Return False
                End If
                TailleFichierWave = FileSize() - FichierAudio.PositionPointer
                Return True
            End If
        End If
        Return False
    End Function
    '----------------------Fonction de fermeture du fichier WAVE------------------------------------
    Public Overrides Function CloseFile() As Boolean
        TailleFichierWave = 0
        Return FichierAudio.CloseFile
    End Function
    '----------------------Fonction de lecture des données--------------------------------------
    Public Overrides Function ReadWaveData(ByVal TailleBuffer As Long) As Byte()
        Position = FichierAudio.PositionPointer()
        If Position = TailleFichierWave Then Return Nothing
        If Position + TailleBuffer > TailleFichierWave Then
            TailleBuffer = TailleFichierWave - Position
        End If
        Return FichierAudio.ReadData(TailleBuffer)
    End Function
    '----------------------Fonction de création d'un fichier WAVE-----------------------------------
    Public Overrides Function CreateFile(ByVal Name As String) As Boolean
        If FileIsOpen Then CloseFile()
        If FichierAudio.CreateNewFile(Name) Then 'System.IO.Path.ChangeExtension(Name, ".wav")) Then
            If WriteFileHeader(lFileHeader) Then Return True
        End If
        Return False
    End Function
    '----------------------Fonction d'écriture de l'entete-----------------------------------
    Public Function WriteFileHeader(ByVal FileHeader As WAVEFILEHEADER) As Boolean
        Try
            Const WAVEFILE_LONGDEFFORMAT = 16
            lFileHeader = FileHeader
            FichierAudio.ChangePointer(0)
            FichierAudio.WriteString("RIFF")
            FichierAudio.WriteInt32(FichierAudio.FileSize)
            FichierAudio.WriteString("WAVEfmt ")
            FichierAudio.WriteInt32(WAVEFILE_LONGDEFFORMAT)
            FichierAudio.WriteInt16(ACM.WAVE_FORMAT_PCM)
            FichierAudio.WriteInt16(lFileHeader.NbrCanaux)
            FichierAudio.WriteInt32(lFileHeader.Frequence)
            FichierAudio.WriteInt32(lFileHeader.NbrOctetsSeconde)
            FichierAudio.WriteInt16(lFileHeader.NbrOctetsEchant)
            FichierAudio.WriteInt16(lFileHeader.NbrBitsEchant)
            FichierAudio.WriteString("data")
            FichierAudio.WriteInt32(FichierAudio.FileSize - Marshal.SizeOf(lFileHeader))
            Return True
        Catch ex As Exception
        End Try
        Return False
    End Function
    '----------------------Fonction d'écriture des données-----------------------------------
    Public Overrides Function WriteData(ByVal Buffer() As Byte) As Boolean
        If Buffer IsNot Nothing Then
            Return FichierAudio.WriteData(Buffer)
        End If
        Return False
    End Function
    '***********************************************************************************************
    '-------------------------------METHODES PRIVEES DE LA CLASSE---------------------------------
    '***********************************************************************************************
    Private Function ReadFileHeader() As Boolean
        Try
            Dim NewFileHeader As WAVEFILEHEADER = New WAVEFILEHEADER
            FichierAudio.ChangePointer(0)
            With NewFileHeader
                .IdFichier = FichierAudio.ReadString(4)
                .TailleFichier = FichierAudio.ReadDMot
                .IdFormatWAV = FichierAudio.ReadString(8)
                .LgDefFormat = FichierAudio.ReadDMot
                .NumFormat = FichierAudio.ReadMot
                .NbrCanaux = FichierAudio.ReadMot
                .Frequence = FichierAudio.ReadDMot
                .NbrOctetsSeconde = FichierAudio.ReadDMot
                .NbrOctetsEchant = FichierAudio.ReadMot
                .NbrBitsEchant = FichierAudio.ReadMot
                .IdDonnees = FichierAudio.ReadString(4)
                .LgDonnees = FichierAudio.ReadDMot
            End With
            lFileHeader = NewFileHeader
            Return True
        Catch ex As Exception
        End Try
        Return False
    End Function

End Class
