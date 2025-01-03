Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend Module GeneratedNames2
        <Extension>
        Friend Function GetKind(compiler As CompilerKind, name As String) As GeneratedNameKind
            Return CommonGeneratedNames.GetKind(compiler, name).ToGeneratedNameKind()
        End Function

        <Extension>
        Friend Function GetKind2(compiler As CompilerKind, name As String) As CommonGeneratedNameKind
            Return CommonGeneratedNames.GetKind(compiler, name)
        End Function

        <Extension>
        Friend Function TryParseGeneratedName(compiler As CompilerKind, name As String, <Out> ByRef kind As CommonGeneratedNameKind, <Out> ByRef part As String) As Boolean
            Dim res = CommonGeneratedNames.TryParseGeneratedName(compiler, name, kind, part)
            Return res
        End Function

        <Extension>
        Friend Function TryParseHoistedUserVariableName(compiler As CompilerKind, proxyName As String, <Out> ByRef variableName As String) As Boolean
            Return CommonGeneratedNames.TryParseHoistedUserVariableName(compiler, proxyName, variableName)
        End Function

        <Extension>
        Friend Function TryParseStateMachineHoistedUserVariableOrDisplayClassName(compiler As CompilerKind, proxyName As String, <Out> ByRef variableName As String, <Out()> ByRef index As Integer) As Boolean
            Return CommonGeneratedNames.TryParseStateMachineHoistedUserVariableOrDisplayClassName(compiler, proxyName, variableName, index)
        End Function

        <Extension>
        Public Function TryParseStateMachineTypeName(compiler As CompilerKind, stateMachineTypeName As String, <Out> ByRef methodName As String) As Boolean
            Return CommonGeneratedNames.TryParseStateMachineTypeName(compiler, stateMachineTypeName, methodName)
        End Function

        <Extension>
        Friend Function TryParseStaticLocalFieldName(compiler As CompilerKind, fieldName As String, <Out> ByRef methodName As String, <Out> ByRef methodSignature As String, <Out> ByRef localName As String) As Boolean
            Return CommonGeneratedNames.TryParseStaticLocalFieldName(compiler, fieldName, methodName, methodSignature, localName)
        End Function

        <Extension>
        Public Function GetUnmangledTypeParameterName(compiler As CompilerKind, typeParameterName As String) As String
            Return CommonGeneratedNames.GetUnmangledTypeParameterName(compiler, typeParameterName)
        End Function

        <Extension>
        Public Function ContainsHoistedMeName(compiler As CompilerKind, dict As ImmutableDictionary(Of String, DisplayClassVariable)) As Boolean
            For Each kv In dict
                If compiler.GetKind(kv.Key) = GeneratedNameKind.HoistedMeField Then
                    Return True
                End If
            Next
            Return False
        End Function

        <Extension>
        Public Function ContainsHoistedMeName(compiler As CompilerKind, list As IEnumerable(Of String)) As String
            For Each name In list
                If compiler.GetKind(name) = GeneratedNameKind.HoistedMeField Then
                    Return True
                End If
            Next
            Return False
        End Function

        <Extension>
        Public Function IsDisplayClassInstance(compiler As CompilerKind, fieldType As String, fieldName As String) As String
            Return CommonGeneratedNames.IsDisplayClassInstance(compiler, fieldType, fieldName)
        End Function
    End Module
End Namespace
