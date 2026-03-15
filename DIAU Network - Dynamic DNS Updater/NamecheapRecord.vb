Imports System.ComponentModel

Public Class NamecheapRecord
    Implements INotifyPropertyChanged

    Private _host As String
    Private _domain As String
    Private _password As String
    Private _lastStatus As String
    Private _lastUpdated As DateTime

    Public Property Host As String
        Get
            Return _host
        End Get
        Set(value As String)
            If _host <> value Then
                _host = value
                OnPropertyChanged(NameOf(Host))
            End If
        End Set
    End Property

    Public Property Domain As String
        Get
            Return _domain
        End Get
        Set(value As String)
            If _domain <> value Then
                _domain = value
                OnPropertyChanged(NameOf(Domain))
            End If
        End Set
    End Property

    Public Property Password As String
        Get
            Return _password
        End Get
        Set(value As String)
            If _password <> value Then
                _password = value
                OnPropertyChanged(NameOf(Password))
            End If
        End Set
    End Property

    Public Property LastStatus As String
        Get
            Return _lastStatus
        End Get
        Set(value As String)
            If _lastStatus <> value Then
                _lastStatus = value
                OnPropertyChanged(NameOf(LastStatus))
            End If
        End Set
    End Property

    Public Property LastUpdated As DateTime
        Get
            Return _lastUpdated
        End Get
        Set(value As DateTime)
            If _lastUpdated <> value Then
                _lastUpdated = value
                OnPropertyChanged(NameOf(LastUpdated))
            End If
        End Set
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Protected Sub OnPropertyChanged(propertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub
End Class

Public Class DdnsConfig
    Public Property Records As List(Of NamecheapRecord)
    Public Property PeriodicEnabled As Boolean
    Public Property PeriodicIntervalMinutes As Integer
End Class
