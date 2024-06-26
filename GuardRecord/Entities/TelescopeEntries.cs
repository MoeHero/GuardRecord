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

	[JsonObject(MemberSerialization.OptIn), Table(Name = "telescope_entries")]
	public partial class TelescopeEntries {

		[JsonProperty, Column(Name = "sequence", DbType = "bigint unsigned", IsPrimary = true, IsIdentity = true)]
		public ulong Sequence { get; set; }

		[JsonProperty, Column(Name = "batch_id")]
		public Guid BatchId { get; set; }

		[JsonProperty, Column(Name = "content", DbType = "longtext")]
		public string Content { get; set; } = string.Empty;

		[JsonProperty, Column(Name = "created_at", DbType = "datetime")]
		public DateTime? CreatedAt { get; set; }

		[JsonProperty, Column(Name = "family_hash")]
		public string FamilyHash { get; set; } = string.Empty;

		[JsonProperty, Column(Name = "should_display_on_index", DbType = "tinyint(1)")]
		public sbyte ShouldDisplayOnIndex { get; set; }

		[JsonProperty, Column(Name = "type", DbType = "varchar(20)")]
		public string Type { get; set; } = string.Empty;

		[JsonProperty, Column(Name = "uuid")]
		public Guid Uuid { get; set; }

	}

}
