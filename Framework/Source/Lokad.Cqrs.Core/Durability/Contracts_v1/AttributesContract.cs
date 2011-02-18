﻿using ProtoBuf;

namespace Lokad.Cqrs
{
	[ProtoContract]
	public sealed class AttributesContract
	{
		[ProtoMember(1, DataFormat = DataFormat.Default)]
		public readonly AttributesItemContract[] Items;

		public AttributesContract(AttributesItemContract[] items)
		{
			Items = items;
		}
		// ReSharper disable UnusedMember.Local
		AttributesContract()
		{
			Items = new AttributesItemContract[0];
		}

		public Maybe<string> GetAttributeString(AttributeTypeContract type)
		{
			for (int i = Items.Length - 1; i >= 0; i--)
			{
				var item = Items[i];
				if (item.Type == type)
				{
					var value = item.StringValue;
					if (value == null)
						throw Errors.InvalidOperation("String attribute can't be null");
					return value;
				}
			}
			return Maybe<string>.Empty;
		}
	}
}