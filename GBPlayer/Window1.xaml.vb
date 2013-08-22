
Public Class Window1
    '*****************************************************************************************************************************
    '**********************************************GESTION DE L'INTERFACE*********************************************************
    '*****************************************************************************************************************************
    Private Sub Entete_Fenetre_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles EnteteFenetre.MouseDown
        If e.ChangedButton = MouseButton.Left Then Me.DragMove()
    End Sub
    'Reponse au message de fermeture de la fenetre
    Private Sub BoutonFermer_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BoutonFermer.Click
        Hide()
    End Sub

    Protected Overrides Sub Finalize()

        MyBase.Finalize()
    End Sub
End Class
