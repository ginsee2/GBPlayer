'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 31/12/11
'DESCRIPTION :Classe d'acces à la libraries Bass
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.Windows.Interop
Imports System.Threading
Imports System.Collections.ObjectModel

Public Class BassSystem
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Private Shared BassInitOk As Integer
    Public Shared ReadOnly Property GetBASSVersion As String
        Get
            Dim valeur = BASS_GetVersion()
            Return HiByte(HiWord(valeur)).ToString("00-") & LoByte(HiWord(valeur)).ToString("00-") & _
                            HiByte(LoWord(valeur)).ToString("00-") & LoByte(LoWord(valeur)).ToString("00")
        End Get
    End Property
    Public Shared ReadOnly Property GetBASS_FXVersion As String
        Get
            Dim valeur = BASS_FX_GetVersion()
            Return HiByte(HiWord(valeur)).ToString("00-") & LoByte(HiWord(valeur)).ToString("00-") & _
                            HiByte(LoWord(valeur)).ToString("00-") & LoByte(LoWord(valeur)).ToString("00")
        End Get
    End Property
    '***********************************************************************************************
    '----------------------------------CONSTRUCTEUR DE LA CLASSE------------------------------------
    '***********************************************************************************************
    Public Sub New(ByVal Frequence As UInt32, DirectXSystemList As ObservableCollection(Of String))
        Try
            If BassInitOk = 0 Then
                Dim Win As WindowInteropHelper = New WindowInteropHelper(Application.Current.MainWindow)
                For i = 1 To DirectXSystemList.Count
                    Dim Retour = BASS_Init(i, Frequence, Enum_Bass_Device.BASS_DEVICE_SPEAKERS, Win.Handle, 0)
                    If Retour = 0 Then Throw New Exception(BASS.BASS_ErrorGetMessage("BASS_Init"))
                Next i
            End If
            BassInitOk += 1
        Catch ex As Exception
        End Try
    End Sub

    '***********************************************************************************************
    '----------------------------------DESTRUCTEUR DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Protected Overrides Sub Finalize()
        BassInitOk -= 1
        If BassInitOk = 0 Then BASS_Free()
        MyBase.Finalize()
    End Sub
End Class
