﻿// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;

namespace Remotion.Infrastructure.Analyzers.BaseCalls
{
  public enum BaseCall
  {
    Default,
    IsOptional,
    IsMandatory
  }

  /// <summary>
  ///   Indicates a specific mode of checking the base call for all derived methods. If the parameter
  ///   <param name="mode">mode</param>
  ///   is set to <see cref="BaseCall.IsMandatory" />, a base call check will be done for all derived methods, if set to
  ///   <see cref="BaseCall.IsOptional" />,
  ///   the base call is optional and therefore no derived method will be checked.
  ///   If the attribute is not set, the default checking mode will be activated: only derived methods returning void are
  ///   checked for missing base call.
  ///   The attribute can be overriden in any derived method to reset the checking mode. It can be revoked on a single method
  ///   (in means of the inheritance hierarchy)
  ///   by the <see cref="IgnoreBaseCallCheckAttribute" /> attribute.
  /// </summary>
  [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
  public class BaseCallCheckAttribute : System.Attribute
  {
    public BaseCallCheckAttribute (BaseCall mode)
    {
    }

    public BaseCallCheckAttribute ()
        : this(BaseCall.IsMandatory)
    {
    }
  }

  /// <summary>
  ///   Disables the <see cref="BaseCallCheckAttribute" /> and its checking mode once for the method it is applied on.
  ///   All derived classes will again follow the checking mode defined by this class or its base class.
  /// </summary>
  [AttributeUsage(AttributeTargets.Method, Inherited = false)]
  public class IgnoreBaseCallCheckAttribute : System.Attribute
  {
  }

  /// <summary>
  /// Indicates that a method should be treated as abstract for base call analysis. The first derived method does not require a 
  /// base call. All the following derived methods again require the base call given by the applicable rules.
  /// </summary>
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
  public class EmptyTemplateMethodAttribute : Attribute
  {
  }
}