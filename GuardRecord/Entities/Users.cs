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

	[JsonObject(MemberSerialization.OptIn), Table(Name = "users")]
	public partial class Users {

		[JsonProperty, Column(Name = "id", DbType = "bigint unsigned", IsPrimary = true, IsIdentity = true)]
		public ulong Id { get; set; }

		/// <summary>
		/// 头像
		/// </summary>
		[JsonProperty, Column(Name = "avatar_url")]
		public string AvatarUrl { get; set; } = string.Empty;

		[JsonProperty, Column(Name = "created_at", DbType = "timestamp")]
		public DateTime? CreatedAt { get; set; }

		[JsonProperty, Column(Name = "is_deleted", DbType = "tinyint(1)")]
		public sbyte IsDeleted { get; set; }

		/// <summary>
		/// 密码
		/// </summary>
		[JsonProperty, Column(Name = "password")]
		public string Password { get; set; } = string.Empty;

		/// <summary>
		/// 房间Id
		/// </summary>
		[JsonProperty, Column(Name = "room_id")]
		public string RoomId { get; set; } = string.Empty;

		/// <summary>
		/// 状态
		/// </summary>
		[JsonProperty, Column(Name = "status")]
		public UsersSTATUS Status { get; set; }

		[JsonProperty, Column(Name = "updated_at", DbType = "timestamp")]
		public DateTime? UpdatedAt { get; set; }

		/// <summary>
		/// 用户名
		/// </summary>
		[JsonProperty, Column(Name = "username")]
		public string Username { get; set; } = string.Empty;

	}

	public enum UsersSTATUS {
		normal = 1, disabled
	}
}
