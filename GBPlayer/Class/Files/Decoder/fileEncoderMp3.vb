Imports System.Text
Imports System.Runtime.InteropServices

Public Class fileEncoderMp3
    '***********************************************************************************************
    '-------------------------------ENUMERATIONS PUBLIQUES------------------------------------------
    '***********************************************************************************************
    Public Enum EnumBitrateEncodeur
        Bitrate_320 = 320
        Bitrate_256 = 256
        Bitrate_192 = 192
        Bitrate_128 = 128
    End Enum

    '***********************************************************************************************
    '-------------------------------STRUCTURES PUBLIQUES--------------------------------------------
    '***********************************************************************************************
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Private beConfig As PBE_CONFIG
    Private EncoderMp3Ouvert As Boolean
    Dim dwSamples As UInt32             'Nombre d'echantillons wav à envoyer a beEncodeChunk
    Dim TailleBufferWave As UInt32           'Taille du buffer mp3
    Dim TailleBufferMp3 As UInt32           'Taille du buffer mp3
    Dim hbeStream As IntPtr             'Handle du convertisseur mp3
    Dim BufferWavePtr As IntPtr
    Dim Buffermp3Ptr As IntPtr
    Public Shared ReadOnly Property GetLAME_ENCVersion() As String
        Get
            Dim strbuf As PBE_VERSION = New PBE_VERSION
            beVersion(strbuf)
            Return strbuf.byDLLMajorVersion.ToString("00-") & strbuf.byDLLMinorVersion.ToString("00-") &
                            strbuf.byMajorVersion.ToString("00-") & strbuf.byMinorVersion.ToString("00")
        End Get
    End Property
    Public Property BitrateEncoder As EnumBitrateEncodeur
        Get
            Return beConfig.dwBitrate
        End Get
        Set(ByVal value As EnumBitrateEncodeur)
            beConfig.dwBitrate = value
        End Set
    End Property

    '***********************************************************************************************
    '----------------------------------CONSTRUCTEUR DE LA CLASSE------------------------------------
    '***********************************************************************************************
    Public Sub New()
        LoadLame_EncConfig()
    End Sub
    '***********************************************************************************************
    '----------------------------------DESTRUCTEUR DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Protected Overrides Sub Finalize()
        CloseEncoderMp3()
        MyBase.Finalize()
    End Sub

    '***********************************************************************************************
    '----------------FONCTIONS POUR CONVERSION BUFFER WAV VERS MP3----------------------------------
    '***********************************************************************************************
    '----------------------Fonction ouvrant le decodeur---------------------------------------------
    Public Function OpenEncoderMp3(ByVal FichierAEncoder As fileAudio) As UInt32
        Dim Retour As UInt32                'Code de retour des fonctions lame_enc
        Try
            If EncoderMp3Ouvert Then Return 0
            beConfig.dwSampleRate = FichierAEncoder.WaveFrequence()
            If FichierAEncoder.WaveNbrDeCanaux = 1 Then
                beConfig.nMode = BE_MP3_MODE_MONO
            Else
                beConfig.nMode = BE_MP3_MODE_STEREO
            End If
            Retour = beInitStream(beConfig, dwSamples, TailleBufferMp3, hbeStream)
            If Retour <> 0 Then Throw New Exception(LAME_ENC_GetErrorMessage(Retour) & " - beInitStream")
            TailleBufferWave = dwSamples * 2
            BufferWavePtr = Marshal.AllocHGlobal(CInt(TailleBufferWave))
            Buffermp3Ptr = Marshal.AllocHGlobal(CInt(TailleBufferMp3))
            EncoderMp3Ouvert = True
            Return TailleBufferWave
        Catch ex As Exception
            CloseEncoderMp3()
            wpfMsgBox.MsgBoxInfo("Erreur encodeur lame", ex.Message)
        End Try
        Return False
    End Function
    '----------------------Fonction de d'encodage des donnes----------------------------------------
    Public Function ConvertBufferWaveToMp3(ByVal BufferWave() As Byte) As Byte()
        Dim Retour As UInt32
        Try
            If EncoderMp3Ouvert Then
                Dim TailleReelleBufferMp3 As UInt32
                If BufferWave IsNot Nothing Then
                    Marshal.Copy(BufferWave, 0, BufferWavePtr, Math.Min(BufferWave.Length, TailleBufferWave))
                    Retour = beEncodeChunk(hbeStream, dwSamples, BufferWavePtr, Buffermp3Ptr, TailleReelleBufferMp3)
                    If Retour <> 0 Then Throw New Exception(LAME_ENC_GetErrorMessage(Retour) & " - beEncodeChunk")
                Else
                    Retour = beDeinitStream(hbeStream, Buffermp3Ptr, TailleReelleBufferMp3)
                    If Retour <> 0 Then Throw New Exception(LAME_ENC_GetErrorMessage(Retour) & " - beDeinitStream")
                End If
                Dim BufferRetour(TailleReelleBufferMp3 - 1) As Byte
                Marshal.Copy(Buffermp3Ptr, BufferRetour, 0, TailleReelleBufferMp3)
                Return BufferRetour
            End If
        Catch ex As Exception
            CloseEncoderMp3()
            wpfMsgBox.MsgBoxInfo("Erreur convertisseur", ex.Message)
        End Try
        Return Nothing
    End Function
    '----------------------Fonction fermant le decodeur---------------------------------------------
    Public Function CloseEncoderMp3() As Boolean
        If BufferWavePtr <> 0 Then
            Marshal.FreeHGlobal(BufferWavePtr)
            BufferWavePtr = 0
        End If
        If Buffermp3Ptr <> 0 Then
            Marshal.FreeHGlobal(Buffermp3Ptr)
            Buffermp3Ptr = 0
        End If
        beCloseStream(hbeStream)
        hbeStream = 0
        EncoderMp3Ouvert = False
        Return True
    End Function

    '***********************************************************************************************
    '-------------------------GESTION DE LA CONFIGURATION ENCODEUR LAME-----------------------------
    '***********************************************************************************************
    '----------------------Methode privee de chargement des paramètres du codeur lame---------------
    Private Sub LoadLame_EncConfig()
        beConfig.dwConfig = BE_CONFIG_LAME
        beConfig.dwStructVersion = CURRENT_STRUCT_VERSION
        beConfig.dwStructSize = CURRENT_STRUCT_SIZE
        beConfig.dwSampleRate = 44100
        beConfig.dwReSampleRate = 0
        beConfig.nMode = BE_MP3_MODE_STEREO
        beConfig.dwBitrate = 320
        beConfig.dwMaxBitrate = 320
        beConfig.nPreset = LQP_NORMAL_QUALITY
        beConfig.dwMpegVersion = MPEG1
        beConfig.bPrivate = 1
        beConfig.bCRC = 0
        beConfig.bCopyright = 0
        beConfig.bOriginal = 0
        beConfig.bWriteVBRHeader = 0
        beConfig.bEnableVBR = 0
        beConfig.nVBRQuality = 1
        beConfig.dwVbrAbr_bps = 0
        beConfig.nVbrMethod = VBR_METHOD_DEFAULT
        beConfig.bNoRes = 1
        beConfig.bStrictIso = 0
    End Sub

End Class
