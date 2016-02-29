﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LLZipLib
{
	public class ZipArchive : Block
	{
		public IStringConverter StringConverter => new DefaultStringConverter();
		public CentralDirectoryFooter CentralDirectoryFooter { get; private set; }
		public IList<ZipEntry> Entries { get; }

		public ZipArchive()
		{
			CentralDirectoryFooter = new CentralDirectoryFooter(this);
			Entries = new List<ZipEntry>();
		}

		public void Read(string filename)
		{
			using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
				Read(stream);
		}

		public void Read(Stream stream)
		{
			using (var reader = new BinaryReader(stream, Encoding.ASCII))
				Read(reader);
		}

		public void Read(BinaryReader reader)
		{
			Offset = reader.BaseStream.Position;
			CentralDirectoryFooter = new CentralDirectoryFooter(this, reader);

			Entries.Clear();
			var headers = new List<CentralDirectoryHeader>();
			reader.BaseStream.Seek(CentralDirectoryFooter.CentralDirectoryOffset, SeekOrigin.Begin);

			for (var i = 0; i < CentralDirectoryFooter.DiskEntries; i++)
				headers.Add(new CentralDirectoryHeader(reader));

			foreach (var header in headers)
				Entries.Add(new ZipEntry(this, reader, header));
		}

		public void Write(string filename)
		{
			using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read))
				Write(stream);
		}

		public void Write(Stream stream)
		{
			using (var writer = new BinaryWriter(stream, Encoding.UTF8))
				Write(writer);
		}

		public void Write(BinaryWriter writer)
		{
			Offset = writer.BaseStream.Position;

			foreach (var entry in Entries)
				entry.Write(writer);

			foreach (var entry in Entries)
				entry.CentralDirectoryHeader.Write(writer);

			CentralDirectoryFooter.Write(writer);

			if (writer.BaseStream.Position - Offset != GetSize())
				throw new NotSupportedException("size mismatch");
		}

		internal override int GetSize()
		{
			return CentralDirectoryFooter.GetSize() +
			       Entries.Sum(entry =>
				       entry.CentralDirectoryHeader.GetSize()
				       + entry.LocalFileHeader.GetSize()
				       + (entry.Data?.Length ?? 0)
				       + (entry.HasDataDescriptor ? entry.DataDescriptor.GetSize() : 0));
		}
	}
}