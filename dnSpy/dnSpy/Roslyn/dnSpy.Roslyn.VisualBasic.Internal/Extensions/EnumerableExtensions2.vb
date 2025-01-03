' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Global.dnSpy.Roslyn.Utilities2
	Module EnumerableExtensions2
		<System.Runtime.CompilerServices.Extension()>
		Public Function WhereNotNull(Of T As Class)(source As IEnumerable(Of T)) As IEnumerable(Of T)
			If source Is Nothing Then
				Return Array.Empty(Of T)()
			End If
			Return source.Where(s_notNullTest)
		End Function
		Private s_notNullTest As Func(Of Object, Boolean) = Function(x As Object) x IsNot Nothing
	End Module
End Namespace
