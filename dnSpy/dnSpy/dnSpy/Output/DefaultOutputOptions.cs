/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Output {
	static class DefaultOutputOptions {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public const string ShowTimestampsName = "Output/ShowTimestamps";
		public static readonly EditorOptionKey<bool> ShowTimestampsId = new EditorOptionKey<bool>(ShowTimestampsName);
		public const string TimestampDateTimeFormatName = "Output/TimestampDateTimeFormat";
		public const string DefaultTimestampDateTimeFormat = "HH:mm:ss.fff";
		public static readonly EditorOptionKey<string> TimestampDateTimeFormatId = new EditorOptionKey<string>(TimestampDateTimeFormatName);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
