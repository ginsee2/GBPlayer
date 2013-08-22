Option Compare Text
'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 26/12/11
'DESCRIPTION :Classe convertisseur mp3 et wave
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Threading

Public Class fileConverter
    Public Enum ConvertType
        wav
        mp3
    End Enum
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    '    Public Event ChangeStatus(ByVal FileName As String, ByVal Description As String)
    Public Event ProcessStart(ByVal FileName As String)
    Public Event ProcessStop(ByVal FileName As String)
    Public Event ProcessProgress(ByVal FileName As String, ByVal value As Integer)

    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Private DerniereErreur As String
    Private Property NbrProcess As Integer
    Private Property NbrProcessSimul As Integer
    Private Property NbrProcessSimulMax As Integer = 1
    Private Shared Fenetre As MainWindow
    Private Shared FenetreInfo As WindowUpdateProgress
    Public ReadOnly Property IsAlive As Boolean
        Get
            If NbrProcess > 0 Then Return True Else Return False
        End Get
    End Property

    '***********************************************************************************************
    '----------------FONCTIONS POUR CONVERSION BUFFER MP3 VERS WAV----------------------------------
    '***********************************************************************************************
    Public Function ConvertToWave(ByVal NomFichier As String, Optional ByVal PathFichier As String = "", Optional ByVal Dossier As String = "") As Boolean
        Dim NomFichierWave As String = ""
        Dim NomFichierAConvertir As String
        Dim FichierAEncoder As fileAudio = Nothing
        NbrProcess += 1
        If NbrProcess > NbrProcessSimulMax Then
            While NbrProcessSimul >= NbrProcessSimulMax
                Debug.Print("Mise en attente conversion " & NbrProcess & " / " & NbrProcessSimulMax)
                Thread.Sleep(100)
            End While
        End If
        NbrProcessSimul += 1
        Thread.Sleep(500)
        RaiseEvent ProcessStart(NomFichier)
        Try
            Select Case Path.GetExtension(NomFichier)
                Case ".mp3"
                    If Not File.Exists(NomFichier) Then Return False
                    FichierAEncoder = New fileMp3
                    If FichierAEncoder.OpenFile(NomFichier) Then
                        NomFichierAConvertir = Path.GetFileNameWithoutExtension(NomFichier)
                    Else
                        Return False
                    End If
                Case ".cd"
                    FichierAEncoder = New fileCdAudio
                    If FichierAEncoder.OpenFile(NomFichier) Then
                        NomFichierAConvertir = Path.GetFileNameWithoutExtension("essai") 'NomFichier)
                    Else
                        Return False
                    End If
                Case Else
                    Return False
            End Select
            If PathFichier = "" Then PathFichier = Path.GetDirectoryName(FichierAEncoder.FileName)
            If Dossier <> "" Then
                If Not My.Computer.FileSystem.DirectoryExists(PathFichier & "\" & Dossier) Then
                    If My.Computer.FileSystem.DirectoryExists(PathFichier) Then
                        My.Computer.FileSystem.CreateDirectory(PathFichier & "\" & Dossier)
                    Else
                        Return False
                    End If
                End If
                PathFichier = PathFichier & "\" & Dossier
            End If
            NomFichierWave = NomFichierAConvertir
            Do Until Not File.Exists(PathFichier & "\" & NomFichierWave & ".wav")
                NomFichierWave = "Copie de " & NomFichierWave
            Loop
            NomFichierWave = PathFichier & "\" & NomFichierWave & ".~tw"
            Dim FichierWave As New fileWave
            Try
                If Not FichierWave.CreateFile(NomFichierWave) Then Return False
                Dim Buffer() As Byte
                Dim MemAvancement As Integer = 0
                Do
                    Buffer = FichierAEncoder.ReadWaveData(FichierAEncoder.PlayBufferSize)
                    Dim Avancement As Integer = CInt(100 * (1 - (FichierAEncoder.FileSize - FichierAEncoder.FilePosition) / FichierAEncoder.FileSize))
                    If Avancement <> MemAvancement Then
                        MemAvancement = Avancement
                        RaiseEvent ProcessProgress(NomFichier, Avancement)
                    End If
                    If Buffer IsNot Nothing Then
                        FichierWave.WriteData(Buffer)
                    End If
                Loop While Buffer IsNot Nothing
                ' FichierWave.WriteFileHeader()
                Dim HeaderFile As fileWave.WAVEFILEHEADER = New fileWave.WAVEFILEHEADER
                HeaderFile.NbrCanaux = FichierAEncoder.WaveNbrDeCanaux
                HeaderFile.Frequence = FichierAEncoder.WaveFrequence
                HeaderFile.NbrOctetsSeconde = FichierAEncoder.WaveOctetParSec
                HeaderFile.NbrOctetsEchant = FichierAEncoder.WaveOctetParEchant
                HeaderFile.NbrBitsEchant = FichierAEncoder.WaveBitsParEchant
                FichierWave.WriteFileHeader(HeaderFile)
                FichierWave.CloseFile()
                FichierAEncoder.CloseFile()
                Rename(NomFichierWave, System.IO.Path.ChangeExtension(NomFichierWave, ".wav"))
                Return True
            Catch ex As Exception
                If FichierWave IsNot Nothing Then FichierWave.CloseFile()
                File.Delete(NomFichierWave)
            End Try
        Catch ex As Exception
        Finally
            NbrProcess -= 1
            NbrProcessSimul -= 1
            RaiseEvent ProcessStop(NomFichier)
            If FichierAEncoder IsNot Nothing Then FichierAEncoder.CloseFile()
        End Try
        Return False
    End Function
    Public Function ConvertToMp3(ByVal NomFichier As String, Optional ByVal PathFichier As String = "",
                                    Optional ByVal Dossier As String = "",
                                    Optional ByVal Bitrate As fileEncoderMp3.EnumBitrateEncodeur = 0) As Boolean
        Dim NomFichierMp3 As String = ""
        Dim NomFichierAConvertir As String
        Dim FichierAEncoder As fileAudio = Nothing
        NbrProcess += 1
        If NbrProcess > NbrProcessSimulMax Then
            While NbrProcessSimul >= NbrProcessSimulMax
                Debug.Print("Mise en attente conversion " & NbrProcess & " / " & NbrProcessSimulMax)
                Thread.Sleep(100)
            End While
        End If
        NbrProcessSimul += 1
        RaiseEvent ProcessStart(NomFichier)
        Try
            Select Case Path.GetExtension(NomFichier)
                Case ".mp3"
                    If Not File.Exists(NomFichier) Then Return False
                    FichierAEncoder = New fileMp3
                    If FichierAEncoder.OpenFile(NomFichier, True) Then
                        NomFichierAConvertir = Path.GetFileNameWithoutExtension(NomFichier)
                    Else
                        Return False
                    End If
                Case ".wav"
                    If Not File.Exists(NomFichier) Then Return False
                    FichierAEncoder = New fileWave
                    If FichierAEncoder.OpenFile(NomFichier, True) Then
                        NomFichierAConvertir = Path.GetFileNameWithoutExtension(NomFichier)
                    Else
                        Return False
                    End If
                Case ".cd"
                    FichierAEncoder = New fileCdAudio
                    If CType(FichierAEncoder, fileCdAudio).OpenFileForConvert(NomFichier) Then
                        NomFichierAConvertir = Path.GetFileNameWithoutExtension(NomFichier)
                    Else
                        Return False
                    End If
                Case Else
                    Return False
            End Select
            If PathFichier = "" Then PathFichier = Path.GetDirectoryName(FichierAEncoder.FileName)
            If Dossier <> "" Then
                If Not My.Computer.FileSystem.DirectoryExists(PathFichier & "\" & Dossier) Then
                    If My.Computer.FileSystem.DirectoryExists(PathFichier) Then
                        My.Computer.FileSystem.CreateDirectory(PathFichier & "\" & Dossier)
                    Else
                        Return False
                    End If
                End If
                PathFichier = PathFichier & "\" & Dossier
            End If
            NomFichierMp3 = NomFichierAConvertir
            Do Until Not File.Exists(PathFichier & "\" & NomFichierMp3 & ".mp3")
                NomFichierMp3 = "Copie de " & NomFichierMp3
            Loop
            NomFichierMp3 = PathFichier & "\" & NomFichierMp3 & ".~t3"
            Dim Encoder As fileEncoderMp3 = New fileEncoderMp3
            If Bitrate <> 0 Then Encoder.BitrateEncoder = Bitrate
            Dim TailleBufferAudio As Integer = Encoder.OpenEncoderMp3(FichierAEncoder)
            Dim FichierMp3 As New fileMp3
            Try
                If Not FichierMp3.CreateFile(NomFichierMp3) Then Return False
                Dim Buffer() As Byte
                Dim MemAvancement As Integer = 0
                Do
                    Buffer = FichierAEncoder.ReadWaveData(TailleBufferAudio)
                    Dim BufferMp3() As Byte = Encoder.ConvertBufferWaveToMp3(Buffer)
                    Dim Avancement As Integer = CInt(100 * (1 - (FichierAEncoder.FileSize - FichierAEncoder.FilePosition) / FichierAEncoder.FileSize))
                    If Avancement <> MemAvancement Then
                        MemAvancement = Avancement
                        RaiseEvent ProcessProgress(NomFichier, Avancement)
                    End If
                    If BufferMp3 IsNot Nothing Then FichierMp3.WriteData(BufferMp3)
                Loop While Buffer IsNot Nothing
                FichierAEncoder.CloseFile()
                FichierMp3.CloseFile()
                Encoder.CloseEncoderMp3()
                Dim NouveauNomMp3 As String = System.IO.Path.ChangeExtension(NomFichierMp3, ".mp3")
                Rename(NomFichierMp3, NouveauNomMp3)
                Using Fichier As New gbDev.TagID3.tagID3Object(NouveauNomMp3)
                    Dim DataConfig As ConfigPerso = ConfigPerso.LoadConfig
                    Dim ChaineExtractionInfos As String = DataConfig.FILESINFOS_ChaineExtractionInfos
                    If ChaineExtractionInfos <> "" Then
                        Fichier.ExtractInfosTitre(ChaineExtractionInfos)
                        Fichier.SaveID3()
                    End If
                End Using
                Return True
            Catch ex As Exception
                If FichierMp3 IsNot Nothing Then FichierMp3.CloseFile()
                File.Delete(NomFichierMp3)
            End Try
        Catch ex As Exception
        Finally
            NbrProcess -= 1
            NbrProcessSimul -= 1
            RaiseEvent ProcessStop(NomFichier)
            If FichierAEncoder IsNot Nothing Then FichierAEncoder.CloseFile()
        End Try
        Return False
    End Function

End Class
