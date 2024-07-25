// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using Microsoft.CodeAnalysis;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

public readonly struct BaseCallDescriptor (Location location, bool callsBaseMethod)
{
  public Location Location { get; } = location;
  public bool CallsBaseMethod { get; } = callsBaseMethod;

  public static bool operator == (BaseCallDescriptor left, BaseCallDescriptor right)
  {
    return left.Equals(right);
  }

  public static bool operator != (BaseCallDescriptor left, BaseCallDescriptor right)
  {
    return !(left == right);
  }

  public override bool Equals (object? o)
  {
    return o is BaseCallDescriptor other && Equals(other);
  }

  private bool Equals (BaseCallDescriptor other)
  {
    return Location == other.Location && CallsBaseMethod == other.CallsBaseMethod;
  }

  public override int GetHashCode ()
  {
    return Location.GetHashCode() ^ CallsBaseMethod.GetHashCode();
  }
}