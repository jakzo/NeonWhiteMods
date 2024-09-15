var frames = new System.Diagnostics.StackTrace().GetFrames();
foreach (var frame in frames)
{
    var m = frame.GetMethod();
    sb.AppendLine((m.DeclaringType != null ? m.DeclaringType.FullName : "") + m.Name);
}
