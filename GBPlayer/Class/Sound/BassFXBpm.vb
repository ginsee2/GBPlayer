'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 31/12/11
'DESCRIPTION : Classe permettant le calcul des bpm - Librairie BASS_FX
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************

Public Class BassFXBpm
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Public Event BassFxNewBpm(ByVal IDFile As String, ByVal Bpm As Single)

    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Private hStreamBPM As IntPtr
    Private IDStream As String
    Private BpmCalculate As Single
    Private CalculBpmEnCours As Boolean

    '***********************************************************************************************
    '----------------------------------CONSTRUCTEUR DE LA CLASSE------------------------------------
    '***********************************************************************************************
    '***********************************************************************************************
    '----------------------------------DESTRUCTION DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    '***********************************************************************************************
    '----------------------------------PROCEDURE PUBLIQUES-------------------------------------
    '***********************************************************************************************
    '---Fonction retournant le BPM - Attention elle est bloquante et doit être faite dans une tache autre.
    '--les BPM sont transmis en cours du calcul par le message "Bass_Fx_NewBpm"
    Public Function GetBPMValue(ByVal FileName As String) As Single
        If Not CalculBpmEnCours Then
            CalculBpmEnCours = True
            IDStream = FileName
            hStreamBPM = BASS_StreamCreateFile(0, FileName, 0, 0, Enum_Bass_Stream.BASS_STREAM_DECODE)
            If hStreamBPM = 0 Then Throw New Exception(BASS_FX.BASS_FX_ErrorGetMessage("BASS_StreamCreateFile"))
            FX_BPM_Calculate()
            BASS_StreamFree(hStreamBPM)
            Return BpmCalculate
        End If
        Return 0
        CalculBpmEnCours = False
    End Function

    '***********************************************************************************************
    '----------------------------------FONCTIONS UTILITAIRES-------------------------------------
    '***********************************************************************************************
    '---FONCTIONS UTILITAIRES PERMETTANT LE CALCUL BPM
    Private Sub FX_BPM_Calculate()
        Try
            If hStreamBPM <> 0 Then
                Dim BpmProc As BPMPROC = AddressOf BPMPROCInterne
                Dim BpmProcessProc As BPMPROCESSPROC = AddressOf BPMPROCESSPROCInterne
                Dim BpmBeatProc As BPMBEATPROC = AddressOf BPMBEATPROCInterne
                Dim retour = BASS_FX_BPM_CallbackSet(hStreamBPM, BpmProc, 30.0, MakeLong(80, 160),
                                    Enum_Bass_Fx_Bpm.BASS_FX_BPM_MULT2, 0)
                If retour = 0 Then Throw New Exception(BASS_FX.BASS_FX_ErrorGetMessage("BASS_FX_BPM_CallbackSet"))
                retour = BASS_FX_BPM_DecodeGet(hStreamBPM, 20.0,
                                                    BASS_ChannelBytes2Seconds(hStreamBPM, BASS_ChannelGetLength(hStreamBPM, 0)) - 20,
                                                    0, Enum_Bass_Fx_Bpm.BASS_FX_BPM_BKGRND Or
                                                    Enum_Bass_Fx_Bpm.BASS_FX_BPM_MULT2 Or
                                                    Enum_Bass_Fx_Bpm.BASS_FX_FREESOURCE,
                                                    BpmProcessProc)
                If retour = -1 Then Throw New Exception(BASS_FX.BASS_FX_ErrorGetMessage("BASS_FX_BPM_DecodeGet"))
                retour = BASS_FX_BPM_BeatSetParameters(hStreamBPM, 10, 50, 20)
                retour = BASS_FX_BPM_BeatDecodeGet(hStreamBPM, 20.0,
                                                    BASS_ChannelBytes2Seconds(hStreamBPM, BASS_ChannelGetLength(hStreamBPM, 0)) - 20,
                                                    Enum_Bass_Fx_Bpm.BASS_FX_BPM_BKGRND Or
                                                    Enum_Bass_Fx_Bpm.BASS_FX_BPM_MULT2 Or
                                                    Enum_Bass_Fx_Bpm.BASS_FX_FREESOURCE,
                                                    BpmBeatProc, 0)
                If retour = 0 Then Throw New Exception(BASS_FX.BASS_FX_ErrorGetMessage("BASS_FX_BPM_BeatDecodeGet"))
            End If
        Catch ex As Exception
            Debug.Print("ERREUR CALCUL BPM " & ex.Message)
            Throw ex
        Finally
            BASS_FX_BPM_BeatFree(hStreamBPM)
            BASS_FX_BPM_Free(hStreamBPM)
        End Try
    End Sub
    Private Sub BPMPROCInterne(ByVal chan As IntPtr, ByVal bpm As Single, ByVal user As UInt32)
        If bpm = 0 Then Exit Sub
        If bpm < 80 Or bpm > 160 Then
            If bpm < 80 Then bpm = bpm * 2
            If bpm > 160 Then bpm = bpm / 2
        End If
        ' If BpmCalculate <> 0 And ((bpm < BpmCalculate * 0.8) Or (bpm > BpmCalculate * 1.2)) Then Exit Sub
        BpmCalculate = bpm
        RaiseEvent BassFxNewBpm(IDStream, bpm)
    End Sub
    Public Sub BPMPROCESSPROCInterne(ByVal chan As IntPtr, ByVal percent As Single)
        '  Debug.Print(percent.ToString)
    End Sub
    Public Sub BPMBEATPROCInterne(ByVal chan As IntPtr, ByVal beatpos As Double, ByVal user As UInt32)
        Debug.Print(beatpos.ToString)
    End Sub

End Class
