Imports System.Runtime.InteropServices

'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 02/01/12
'DESCRIPTION :Module d'importation des fonction librairie BASS_FX

Module BASS_FX
    '*************************************************************************
    '----------------------ENUMERATION UTILISEE EN INTERNE--------------------
    '*************************************************************************
    Public Enum Enum_Bass_Fx_Bpm
        BASS_FX_BPM_BKGRND = 1   ' if in use, then you can do other processing while detection's in progress. (BPM/Beat)
        BASS_FX_BPM_MULT2 = 2    ' if in use, then will auto multiply bpm by 2 (if BPM < minBPM*2)
        BASS_FX_FREESOURCE = &H10000     ' Free the source handle as well?
    End Enum

    '***********************************************************************************************
    '-------------------------------FONCTIONS IMPORTEES---------------------------------------------
    '***********************************************************************************************
    '---Importation de la librairie BASS_FX
    <DllImport("bass_fx.dll")> _
    Public Function BASS_FX_GetVersion() As UInt32
    End Function
    <DllImport("bass_fx.dll")> _
    Public Function BASS_FX_BPM_CallbackSet(ByVal handle As IntPtr, ByVal proc As BPMPROC,
                                                   ByVal period As Double, ByVal minMaxBPM As UInt32,
                                                   ByVal flags As Enum_Bass_Fx_Bpm, ByVal user As UInt32) As Int32
    End Function
    <DllImport("bass_fx.dll")> _
    Public Function BASS_FX_BPM_DecodeGet(ByVal chan As IntPtr, ByVal startSec As Double,
                                                   ByVal endSec As Double, ByVal minMaxBPM As UInt32,
                                                   ByVal flags As Enum_Bass_Fx_Bpm, ByVal proc As BPMPROCESSPROC) As Single
    End Function
    <DllImport("bass_fx.dll")> _
    Public Function BASS_FX_BPM_CallbackReset(ByVal handle As IntPtr) As Int32
    End Function
    <DllImport("bass_fx.dll")> _
    Public Sub BASS_FX_BPM_Free(ByVal handle As IntPtr)
    End Sub
    <DllImport("bass_fx.dll")> _
    Public Function BASS_FX_BPM_BeatDecodeGet(ByVal handle As IntPtr, ByVal startSec As Double,
                                                   ByVal endSec As Double, ByVal flags As Enum_Bass_Fx_Bpm,
                                                   ByVal proc As BPMBEATPROC, ByVal user As UInt32) As UInt32
    End Function
    <DllImport("bass_fx.dll")> _
    Public Function BASS_FX_BPM_BeatCallbackReset(ByVal handle As IntPtr) As Int32
    End Function
    <DllImport("bass_fx.dll")> _
    Public Sub BASS_FX_BPM_BeatFree(ByVal handle As IntPtr)
    End Sub
    <DllImport("bass_fx.dll")> _
    Public Function BASS_FX_BPM_BeatSetParameters(ByVal handle As IntPtr, ByRef bandwidth As Single,
                                                                             ByRef centerfreq As Single,
                                                                             ByRef beat_rtime As Single) As UInt32
    End Function
    <DllImport("bass_fx.dll")> _
    Public Function BASS_FX_BPM_BeatGetParameters(ByVal handle As IntPtr, ByRef bandwidth As Single,
                                                                             ByRef centerfreq As Single,
                                                                             ByRef beat_rtime As Single) As UInt32
    End Function

    Public Delegate Sub BPMBEATPROC(ByVal chan As IntPtr, ByVal beatpos As Double, ByVal user As UInt32)
    Public Delegate Sub BPMPROCESSPROC(ByVal chan As IntPtr, ByVal percent As Single)
    Public Delegate Sub BPMPROC(ByVal chan As IntPtr, ByVal bpm As Single, ByVal user As UInt32)

    '---FONCTION UTILITAIRES PERMETTANT DE TESTER LE CODE DE RETOUR
    Private Enum Enum_Bass_FX_Error
        BASS_ERROR_FX_NODECODE = 4000    ' Not a decoding channel
        BASS_ERROR_FX_BPMINUSE = 4001    ' BPM/Beat detection is in use
    End Enum
    Public Function BASS_FX_ErrorGetMessage(ByVal FonctionOrigine As String) As String
        Dim msg As String
        Select Case BASS_ErrorGetCode()
            Case Enum_Bass_FX_Error.BASS_ERROR_FX_NODECODE
                msg = "Not a decoding channel"
            Case Enum_Bass_FX_Error.BASS_ERROR_FX_BPMINUSE
                msg = "BPM/Beat detection is in use"
            Case Else
                msg = BASS_ErrorGetMessage(FonctionOrigine)
        End Select
        Return msg & " - " & FonctionOrigine
    End Function

End Module
