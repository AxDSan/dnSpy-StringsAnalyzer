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

using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.Menus {
	/// <summary>
	/// Menu item base class, implements <see cref="IMenuItem"/>
	/// </summary>
	public abstract class MenuItemBase : IMenuItem {
		/// <inheritdoc/>
		public abstract void Execute(IMenuItemContext context);
		/// <inheritdoc/>
		public virtual bool IsEnabled(IMenuItemContext context) => true;
		/// <inheritdoc/>
		public virtual bool IsVisible(IMenuItemContext context) => true;
		/// <inheritdoc/>
		public virtual string? GetHeader(IMenuItemContext context) => null;
		/// <inheritdoc/>
		public virtual ImageReference? GetIcon(IMenuItemContext context) => null;
		/// <inheritdoc/>
		public virtual string? GetInputGestureText(IMenuItemContext context) => null;
		/// <inheritdoc/>
		public virtual bool IsChecked(IMenuItemContext context) => false;
	}

	/// <summary>
	/// Menu item base class, implements <see cref="IMenuItem"/>
	/// </summary>
	/// <typeparam name="TContext">Context type</typeparam>
	public abstract class MenuItemBase<TContext> : IMenuItem where TContext : class {
		/// <summary>
		/// Creates the context
		/// </summary>
		/// <param name="context">Menu item context</param>
		/// <returns></returns>
		protected abstract TContext? CreateContext(IMenuItemContext context);

		/// <summary>
		/// Gets the context key. Should be a unique value per class, eg. an <see cref="object"/>
		/// </summary>
		protected abstract object CachedContextKey { get; }

		/// <summary>
		/// Gets the cached context
		/// </summary>
		/// <param name="context">Menu item context</param>
		/// <returns></returns>
		protected TContext? GetCachedContext(IMenuItemContext context) {
			var key = CachedContextKey;
			if (key is null)
				return CreateContext(context);

			return context.GetOrCreateState<TContext>(key, () => CreateContext(context)!);
		}

		void IMenuItem.Execute(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			if (ctx is not null)
				Execute(ctx);
		}

		bool IMenuItem.IsEnabled(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			return ctx is not null && IsEnabled(ctx);
		}

		bool IMenuItem.IsVisible(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			return ctx is not null && IsVisible(ctx);
		}

		string? IMenuItem.GetHeader(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			return ctx is not null ? GetHeader(ctx) : null;
		}

		ImageReference? IMenuItem.GetIcon(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			return ctx is not null ? GetIcon(ctx) : null;
		}

		string? IMenuItem.GetInputGestureText(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			return ctx is not null ? GetInputGestureText(ctx) : null;
		}

		bool IMenuItem.IsChecked(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			return ctx is not null ? IsChecked(ctx) : false;
		}

		/// <summary>
		/// Executes the command
		/// </summary>
		/// <param name="context">Context</param>
		public abstract void Execute(TContext context);

		/// <summary>
		/// Returns true if it's enabled
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		public virtual bool IsEnabled(TContext context) => true;

		/// <summary>
		/// Returns true if it's visible. If false, none of the other methods get called
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		public virtual bool IsVisible(TContext context) => true;

		/// <summary>
		/// Returns the header or null to use the default value from the attribute
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		public virtual string? GetHeader(TContext context) => null;

		/// <summary>
		/// Returns the icon or null to use the default value from the attribute
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		public virtual ImageReference? GetIcon(TContext context) => null;

		/// <summary>
		/// Returns the input gesture text or null to use the default value from the attribute
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		public virtual string? GetInputGestureText(TContext context) => null;

		/// <summary>
		/// Returns true if it's checked
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		public virtual bool IsChecked(TContext context) => false;
	}
}
