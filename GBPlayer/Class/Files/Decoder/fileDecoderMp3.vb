Option Compare Text
'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 26/12/11
'DESCRIPTION :Classe convertisseur mp3
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.Runtime.InteropServices
Imports System.IO

Public Class fileDecoderMp3
 
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Private StreamHeader As ACM.ACMSTREAMHEADER         'Structure pour la conversion ACM
    Dim DecoderMp3Ouvert As Boolean 'Variable indiquant que le codeur mp3 est ouvert et utilisé
    Private FormatWave As MMWAVEFORMATEX                'Structure d'initialisation de la conversion
    Private FormatMpegLayer3 As MPEGLAYER3WAVEFORMAT  'Structure d'initialisation de la conversion
    Dim handleDecoder As Long  'handle du systeme de decodage acmstream

    '***********************************************************************************************
    '----------------------------------DESTRUCTEUR DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Protected Overrides Sub Finalize()
        CloseDecoderMp3()
        MyBase.Finalize()
    End Sub

    '***********************************************************************************************
    '----------------FONCTIONS POUR CONVERSION BUFFER MP3 VERS WAV----------------------------------
    '***********************************************************************************************
    Public Event ChangeStatus(ByVal Description As String)
    '----------------------Fonction ouvrant le decodeur et retournant la taille du buffer wave------
    Public Function OpenDecoderMp3(ByVal FichierMp3 As fileMp3) As Long
        Dim Retour As UInt32              'Code de retour des fonctions codec
        Dim dwMP3Buffer As Integer         'Taille du buffer mp3
        Dim dwWAVBuffer As Integer         'Taille du buffer wav
        Try
            If DecoderMp3Ouvert Then Return 0
            If Not InitStructuresFormat(FichierMp3) Then Return 0
            DecoderMp3Ouvert = True
            Retour = acmStreamOpenMp3(handleDecoder, 0, FormatMpegLayer3, FormatWave, 0, 0, 0, ACM_STREAMOPENF_NONREALTIME)
            If Retour <> 0 Then Throw New Exception(ACM_GetERRORMessage(Retour) & " - acmStreamOpen")
            dwMP3Buffer = FichierMp3.Mp3FrameSize()
            Retour = acmStreamSize(handleDecoder, dwMP3Buffer, dwWAVBuffer, ACM_STREAMSIZEF_SOURCE)
            If Retour <> 0 Then Throw New Exception(ACM_GetERRORMessage(Retour) & " - acmStreamSize")
            StreamHeader.cbStruct = Marshal.SizeOf(StreamHeader)
            StreamHeader.pbSrc = Marshal.AllocHGlobal(dwMP3Buffer)
            StreamHeader.cbSrcLength = dwMP3Buffer
            StreamHeader.cbDstLength = dwWAVBuffer
            StreamHeader.pbDst = Marshal.AllocHGlobal(dwWAVBuffer)
            '            StreamHeader.cbDstLength = dwWAVBuffer
            Retour = acmStreamPrepareHeader(handleDecoder, StreamHeader, 0)
            If Retour <> 0 Then Throw New Exception(ACM_GetERRORMessage(Retour) & " - acmStreamPrepareHeader")
            AttenteSystemePret(ACMSTREAMHEADER_STATUSF_PREPARED)
            Return dwWAVBuffer
        Catch ex As Exception
            CloseDecoderMp3()
            wpfMsgBox.MsgBoxInfo("Erreur decodeur", ex.Message)
            Return 0
        End Try
    End Function
    '----------------------Fonction de conversion d'un buffer mp3 en wave---------------------------
    Public Function ConvertBufferMp3ToWav(ByVal BufferMp3() As Byte) As Byte()
        Dim Retour As UInt32
        Try
            If DecoderMp3Ouvert Then
                Marshal.Copy(BufferMp3, 0, StreamHeader.pbSrc, Math.Min(BufferMp3.Length, StreamHeader.cbSrcLength))
                Retour = acmStreamConvert(handleDecoder, StreamHeader, ACM_STREAMCONVERTF_BLOCKALIGN)
                If Retour <> 0 Then Throw New Exception(ACM_GetERRORMessage(Retour) & " - acmStreamConvert")
                AttenteSystemePret(ACMSTREAMHEADER_STATUSF_DONE)
                Dim TailleBufferWave As Integer = StreamHeader.cbDstLengthUsed
                Dim BufferRetour(TailleBufferWave - 1) As Byte
                Marshal.Copy(StreamHeader.pbDst, BufferRetour, 0, TailleBufferWave)
                Return BufferRetour
            End If
        Catch ex As Exception
            CloseDecoderMp3()
            wpfMsgBox.MsgBoxInfo("Erreur convertisseur", ex.Message)
        End Try
        Return Nothing
    End Function
    '----------------------Fonction fermant le decodeur---------------------------------------------
    Public Function CloseDecoderMp3() As Boolean
        Try
            If StreamHeader.pbDst <> 0 Then
                Call acmStreamUnprepareHeader(handleDecoder, StreamHeader, 0)
                Call acmStreamClose(handleDecoder, 0)
            End If
            If StreamHeader.pbSrc <> 0 Then
                Marshal.FreeHGlobal(StreamHeader.pbSrc)
                StreamHeader.pbSrc = 0
            End If
            If StreamHeader.pbDst <> 0 Then
                Marshal.FreeHGlobal(StreamHeader.pbDst)
                StreamHeader.pbDst = 0
            End If
            DecoderMp3Ouvert = False
            Return True
        Catch ex As Exception
        End Try
    End Function
    '-------------Methode privee d'initialisation des structures acm--------------------------------
    Private Function InitStructuresFormat(ByVal Fichier As fileMp3) As Boolean
        'init structure MMWAVEFORMATEX
        FormatWave.wFormatTag = WAVE_FORMAT_PCM
        FormatWave.nChannels = Fichier.WaveNbrDeCanaux
        FormatWave.nSamplesPerSec = Fichier.WaveFrequence
        FormatWave.nAvgBytesPerSec = Fichier.WaveOctetParSec
        FormatWave.wBitsPerSample = Fichier.WaveBitsParEchant
        FormatWave.nBlockAlign = Fichier.WaveOctetParEchant
        FormatWave.cbSize = 0
        'init structure MPEGLAYER3WAVEFORMAT
        FormatMpegLayer3.wFormatTag = CShort(WAVE_FORMAT_MPEGLAYER3)
        FormatMpegLayer3.nChannels = Fichier.WaveNbrDeCanaux
        FormatMpegLayer3.nSamplesPerSec = Fichier.WaveFrequence
        FormatMpegLayer3.nAvgBytesPerSec = 16384
        FormatMpegLayer3.wBitsPerSample = 0
        FormatMpegLayer3.nBlockAlign = 1
        FormatMpegLayer3.cbSize = MPEGLAYER3_WFX_EXTRA_BYTES
        FormatMpegLayer3.wID = MPEGLAYER3_ID_MPEG
        FormatMpegLayer3.fdwFlags = MPEGLAYER3_FLAG_PADDING_ISO
        FormatMpegLayer3.nBlockSize = 1045 ' Fichier.Mp3FrameSize()
        FormatMpegLayer3.nFramesPerBlock = 1
        FormatMpegLayer3.nCodecDelay = 1393
        InitStructuresFormat = True
    End Function
    '----------Fonction d'attente du traitement par le codec----------------------------------------
    Protected Function AttenteSystemePret(ByVal cbFlag As Long) As Boolean
        Do Until (((StreamHeader.dwStatus And cbFlag) = cbFlag) Or _
                   (StreamHeader.dwStatus = 0))
            wpfApplication.DoEvents()
        Loop
        AttenteSystemePret = True
    End Function

End Class
