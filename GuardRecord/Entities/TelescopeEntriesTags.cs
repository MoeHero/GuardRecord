//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具 FreeSql.Generator 生成。
//     运行时版本:3.1.32
//     Website: https://github.com/2881099/FreeSql
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FreeSql.DataAnnotations;
namespace GuardRecord.Entities {

	[JsonObject(MemberSerialization.OptIn), Table(Name = "telescope_entries_tags")]
	public partial class TelescopeEntriesTags {

		[JsonProperty, Column(Name = "entry_uuid")]
		public Guid EntryUuid { get; set; }

		[JsonProperty, Column(Name = "tag")]
		public string Tag { get; set; } = string.Empty;

	}

}