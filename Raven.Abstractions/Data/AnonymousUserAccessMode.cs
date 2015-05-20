//-----------------------------------------------------------------------
// <copyright file="AnonymousUserAccessMode.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Raven.Abstractions.Data
{
	public enum AnonymousUserAccessMode
	{
		All,
		Get,
		None,
		Admin
	}
}
