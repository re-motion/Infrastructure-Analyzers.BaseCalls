// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT
using Microsoft.CodeAnalysis;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

public class Rules
{
  private const string c_category = "Usage";
  private const DiagnosticSeverity c_severity = DiagnosticSeverity.Warning;

  private const string c_diagnosticId = "RMBCA0001";
  private static readonly LocalizableString s_title = "Base Call missing";
  private static readonly LocalizableString s_messageFormat = "Base Call missing";
  private static readonly LocalizableString s_description = "Base Call is missing.";

  public static readonly DiagnosticDescriptor NoBaseCall = new(
      c_diagnosticId,
      s_title,
      s_messageFormat,
      c_category,
      c_severity,
      true,
      s_description);


  private const string c_diagnosticIdLoopMessage = "RMBCA0002";
  private static readonly LocalizableString s_titleLoopMessage = "Base Call found in a loop";
  private static readonly LocalizableString s_messageFormatLoopMessage = "Base Call found in a loop";
  private static readonly LocalizableString s_descriptionLoopMessage = "Base Call found in a loop, not allowed here.";

  public static readonly DiagnosticDescriptor InLoop = new(
      c_diagnosticIdLoopMessage,
      s_titleLoopMessage,
      s_messageFormatLoopMessage,
      c_category,
      c_severity,
      true,
      s_descriptionLoopMessage);

  private const string c_diagnosticIdAnonymousMethod = "RMBCA0003";
  private static readonly LocalizableString s_titleAnonymousMethod = "Base Call found in anonymous method";
  private static readonly LocalizableString s_messageFormatAnonymousMethod = "Base Call is not allowed in anonymous methods";
  private static readonly LocalizableString s_descriptionAnonymousMethod = "Base Calls should not be used in anonymous methods.";

  public static readonly DiagnosticDescriptor InAnonymousMethod = new(
      c_diagnosticIdAnonymousMethod,
      s_titleAnonymousMethod,
      s_messageFormatAnonymousMethod,
      c_category,
      c_severity,
      true,
      s_descriptionAnonymousMethod);

  private const string c_diagnosticIdLocalFunction = "RMBCA0004";
  private static readonly LocalizableString s_titleLocalFunction = "Base Call found in local function";
  private static readonly LocalizableString s_messageFormatLocalFunction = "Base Call is not allowed in local function";
  private static readonly LocalizableString s_descriptionLocalFunction = "Base Calls should not be used in local function.";

  public static readonly DiagnosticDescriptor InLocalFunction = new(
      c_diagnosticIdLocalFunction,
      s_titleLocalFunction,
      s_messageFormatLocalFunction,
      c_category,
      c_severity,
      true,
      s_descriptionLocalFunction);


  private const string c_diagnosticIdMultipleBaseCalls = "RMBCA0005";
  private static readonly LocalizableString s_titleMultipleBaseCalls = "multiple BaseCalls found";
  private static readonly LocalizableString s_messageMultipleBaseCalls = "multiple BaseCalls found";
  private static readonly LocalizableString s_descriptionMultipleBaseCalls = "multiple BaseCalls found in this method, there should only be one BaseCall.";

  public static readonly DiagnosticDescriptor MultipleBaseCalls = new(
      c_diagnosticIdMultipleBaseCalls,
      s_titleMultipleBaseCalls,
      s_messageMultipleBaseCalls,
      c_category,
      c_severity,
      true,
      s_descriptionMultipleBaseCalls);

  private const string c_diagnosticIdWrongBaseCall = "RMBCA0006";
  private static readonly LocalizableString s_titleWrongBaseCall = "incorrect BaseCall";
  private static readonly LocalizableString s_messageWrongBaseCall = "BaseCall does not call the overridden Method";
  private static readonly LocalizableString s_descriptionWrongBaseCall = "BaseCall does not call the overridden Method.";

  public static readonly DiagnosticDescriptor WrongBaseCall = new(
      c_diagnosticIdWrongBaseCall,
      s_titleWrongBaseCall,
      s_messageWrongBaseCall,
      c_category,
      c_severity,
      true,
      s_descriptionWrongBaseCall);

  private const string c_diagnosticIdInTryOrCatch = "RMBCA0007";
  private static readonly LocalizableString s_titleInTryOrCatch = "BaseCall in Try or Catch block";
  private static readonly LocalizableString s_messageInTryOrCatch = "BaseCall is not allowed in Try or Catch block";
  private static readonly LocalizableString s_descriptionInTryOrCatch = "BaseCall is not allowed in Try or Catch block.";

  public static readonly DiagnosticDescriptor InTryOrCatch = new(
      c_diagnosticIdInTryOrCatch,
      s_titleInTryOrCatch,
      s_messageInTryOrCatch,
      c_category,
      c_severity,
      true,
      s_descriptionInTryOrCatch);

  private const string c_diagnosticIdInInNonOverridingMethod = "RMBCA0008";
  private static readonly LocalizableString s_titleInInNonOverridingMethod = "BaseCall in non overriding Method";
  private static readonly LocalizableString s_messageInInNonOverridingMethod = "BaseCall is not allowed in non overriding Method";
  private static readonly LocalizableString s_descriptionInInNonOverridingMethod = "BaseCall is not allowed in non overriding Method.";

  public static readonly DiagnosticDescriptor InNonOverridingMethod = new(
      c_diagnosticIdInInNonOverridingMethod,
      s_titleInInNonOverridingMethod,
      s_messageInInNonOverridingMethod,
      c_category,
      c_severity,
      true,
      s_descriptionInInNonOverridingMethod);

  private const string c_diagnosticIdSwitch = "RMBCA0009";
  private static readonly LocalizableString s_titleSwitch = "Base Call found in Switch";
  private static readonly LocalizableString s_messageFormatSwitch = "Base Call is not allowed in Switch";
  private static readonly LocalizableString s_descriptionSwitch = "Base Calls should not be used in Switch.";

  public static readonly DiagnosticDescriptor InSwitch = new(
      c_diagnosticIdSwitch,
      s_titleSwitch,
      s_messageFormatSwitch,
      c_category,
      c_severity,
      true,
      s_descriptionSwitch);

  private const string c_diagnosticIdError = "RMBCA0000";
  private static readonly LocalizableString s_titleError = "Error";
  private static readonly LocalizableString s_messageError = "Error: {0}";
  private static readonly LocalizableString s_descriptionError = "Error.";

  public static readonly DiagnosticDescriptor Error = new(
      c_diagnosticIdError,
      s_titleError,
      s_messageError,
      c_category,
      c_severity,
      true,
      s_descriptionError);
}