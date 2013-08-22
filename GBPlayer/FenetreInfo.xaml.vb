Imports System.Collections.ObjectModel

Public Class FenetreInfo
    Private _InfosCollection As New ObservableCollection(Of String)
    Public ReadOnly Property InfosCollection As ObservableCollection(Of String)
        Get
            Return _InfosCollection
        End Get
    End Property
    '*****************************************************************************************************************************
    '**********************************************GESTION DE L'INTERFACE*********************************************************
    '*****************************************************************************************************************************
    ' Private Sub Entete_Fenetre_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles EnteteFenetre.MouseDown
    '     If e.ChangedButton = MouseButton.Left Then Me.DragMove()
    ' End Sub
    'Reponse au message de fermeture de la fenetre
    ' Private Sub BoutonFermer_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BoutonFermer.Click
    '     Hide()
    ' End Sub
    Public Sub addInfos(ByVal Info As String)
        _InfosCollection.Insert(0, Info)
    End Sub
    Protected Overrides Sub Finalize()

        MyBase.Finalize()
    End Sub

    Private Sub FenetreInfo_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Loaded
        Me.BeginStoryboard(FindResource("AffichageProgressif"))
    End Sub
End Class
