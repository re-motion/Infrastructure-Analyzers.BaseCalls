// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT
namespace Remotion.Infrastructure.Analyzers.BaseCalls;

public struct NumberOfBaseCalls (int min, int max)
{
  public static NumberOfBaseCalls Returns => new(-1);
  public static NumberOfBaseCalls DiagnosticFound => new(-2);
  public int Min { get; set; } = min;
  public int Max { get; set; } = max;

  public NumberOfBaseCalls (int numberOfBaseCalls)
      : this(numberOfBaseCalls, numberOfBaseCalls)
  {
  }

  public void Increment ()
  {
    Min++;
    Max++;
  }

  public static bool operator == (NumberOfBaseCalls left, NumberOfBaseCalls right)
  {
    return left.Equals(right);
  }

  public static bool operator != (NumberOfBaseCalls left, NumberOfBaseCalls right)
  {
    return !(left == right);
  }

  public override bool Equals (object? o)
  {
    return o is NumberOfBaseCalls other && Equals(other);
  }

  private bool Equals (NumberOfBaseCalls other)
  {
    return Min == other.Min && Max == other.Max;
  }

  public override int GetHashCode ()
  {
    unchecked
    {
      return (Min * 397) ^ Max;
    }
  }
}