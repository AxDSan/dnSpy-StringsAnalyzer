// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace dnSpy.Roslyn.Debugger.ExpressionCompiler.CSharp {
	static class GeneratedNames {
		// Extracts the slot index from a name of a field that stores hoisted variables or awaiters.
		// Such a name ends with "__{slot index + 1}".
		// Returned slot index is >= 0.
		internal static bool TryParseSlotIndex(string fieldName, out int slotIndex) {
			int lastUnder = fieldName.LastIndexOf('_');
			if (lastUnder - 1 < 0 || lastUnder == fieldName.Length || fieldName[lastUnder - 1] != '_') {
				slotIndex = -1;
				return false;
			}

			if (int.TryParse(fieldName.Substring(lastUnder + 1), NumberStyles.None, CultureInfo.InvariantCulture, out slotIndex) && slotIndex >= 1) {
				slotIndex--;
				return true;
			}

			slotIndex = -1;
			return false;
		}
	}

	static class GeneratedNamesHelpers {
		public static bool TryGetHoistedLocalSlotIndex(string name, out int slotIndex) {
			if (GeneratedNames.TryParseSlotIndex(name, out slotIndex))
				return true;
			slotIndex = -1;
			return false;
		}
	}
}
