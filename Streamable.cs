﻿using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace comroid.common.csapi;

// todo: move to comroid.common source
public abstract class Streamable
{
    public static readonly Regex UnmanagedMemoryStreamPattern = new($@"^(0x[0-9a-f]{{{IntPtr.Size * 2}}}) (\d{{{1},{long.MaxValue.ToString().Length}}})$");
    protected readonly string desc;

    protected Streamable(string desc = null!)
    {
        this.desc = desc;
    }

    public abstract TextReader AsReader(object? arg = null);
    public abstract TextWriter AsWriter(object? arg = null);

    public virtual string AsString(object? _ = null) => AsReader().ReadToEnd();

    public static Streamable Get(string? desc = null) => desc != null
        ? UnmanagedMemoryStreamPattern.IsMatch(desc) ? new UnmanagedMemoryStreamable(desc) :
        File.Exists(desc) ? new FileStreamable(desc) : new StringStreamable(desc)
        : new StdIoStreamable();

    public override string ToString() => desc;

    public abstract class TStreamable<T> : Streamable where T : struct
    {
        protected TStreamable(string desc = null!) : base(desc)
        {
        }

        public abstract TextReader AsReader(T? arg = null);
        public override TextReader AsReader(object? arg = null) => AsReader((T?)arg);
        public abstract TextWriter AsWriter(T? arg = null);
        public override TextWriter AsWriter(object? arg = null) => AsWriter((T?)arg);
    }

    public class StdIoStreamable : TStreamable<bool>
    {
        public override TextReader AsReader(bool? _ = null) => Console.In;
        public override TextWriter AsWriter(bool? arg = null) => arg.HasValue && arg.Value ? Console.Error : Console.Out;
    }

    public class StringStreamable : Streamable
    {
        public StringStreamable(string desc) : base(desc)
        {
        }

        public override StringReader AsReader(object? _ = null) => new(desc);
        public override StringWriter AsWriter(object? _ = null) => new();
        public override string AsString(object? _ = null) => desc;
    }

    public class FileStreamable : TStreamable<(FileMode mode, FileAccess access)>
    {
        public FileStreamable(string desc) : base(desc)
        {
        }

        public override TextReader AsReader((FileMode mode, FileAccess access)? perm = null)
            => new StreamReader(new FileStream(desc, perm?.mode ?? FileMode.Open));
        public override TextWriter AsWriter((FileMode mode, FileAccess access)? perm = null)
            => new StreamWriter(new FileStream(desc, perm?.mode ?? FileMode.Truncate, perm?.access ?? FileAccess.Write));
    }

    public class UnmanagedMemoryStreamable : Streamable
    {
        private unsafe readonly byte* adr;
        private readonly long len;

        public unsafe UnmanagedMemoryStreamable(string desc) : base(desc)
        {
            if (UnmanagedMemoryStreamPattern.Match(desc) is not { Success: true } match)
                throw new FormatException("Could not parse " + desc);
            adr = (byte*)nint.Parse(match.Groups[1].Value);
            len = long.Parse(match.Groups[2].Value);
        }

        public override unsafe TextReader AsReader(object? _ = null) => new StreamReader(new UnmanagedMemoryStream(adr, len));
        public override unsafe TextWriter AsWriter(object? _ = null) => new StreamWriter(new UnmanagedMemoryStream(adr, len));
    }
}