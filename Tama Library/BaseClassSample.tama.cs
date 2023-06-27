// Copyright © 2011 Triamec Motion AG

using Triamec.TriaLink;
using Triamec.Tama.Vmid5;
//using Triamec.Tama.Rlid4;
using Triamec.Tama.Rlid19;

/* Tama Program Variation Sample
 * -----------------------------
 * 
 * You have just written a great Tama program with lots of functionality. Given some slightly different hardware, this
 * might affect some of the defined constants. Given there is a requirement that the drives act stand-alone and don't
 * need to be configured first at start-up.
 * 
 * You end up in copying the Tama program, changing the constants and start to maintain two almost identical programs.
 * 
 * But wait – there is another approach. Put common functionality in a base class and use this class from the different
 * Tama program variations. This file demonstrates how to achieve such a behavior.
 * 
 * The TamaCompiler will produce the Tama programs "Tama Library.BaseTest1" and "Tama Library.BaseTest2".
 * The performance of the code is slighly below that of the original program due to the addtitional call at the
 * beginning of the tasks.
 * 
 * Note: You cannot use this approach to write single source code supporting devices with similar register layouts.
 * To achieve this goal, you need to define symbols and compile the same code multiple times using variating conditional
 * compilation symbols. This is the same as creating two C# projects including the same sources, but declaring different
 * conditional compilation symbols in their Build property page. This approach could also be used for an alternate
 * implementation of the code below. Look at the Homing sample for more information.
 *
 */

class BaseTest {
	// *** Here go all the constants unaffected from different hardware *** 

	// Warning: static field initializers are not allowed here. Use constants or instance fields instead.
	const int BaseConst = 4;
	readonly int _baseField = 7;

	/// <summary>Hardware specific constant.</summary>
	readonly int _myConst;
	internal BaseTest(int myConst) {
        _myConst = myConst;
	}
	public void IsochronousPart() {

		// *** Here goes all the functionality *** 
		Register.Application.Variables.Integers[0] = _myConst;
        Register.Application.Variables.Integers[1] = BaseConst;
        Register.Application.Variables.Integers[2] = _baseField;

    }
}

[Tama]
public static class BaseTest1 {
	const int MyConst = 6;
	static readonly BaseTest BaseTest = new BaseTest(MyConst);

    #region isochronous Tama main application
    [TamaTask(Task.IsochronousMain)]
    public static void IsochronousTamaMain() => BaseTest.IsochronousPart();
    #endregion isochronous Tama Main application
}

[Tama]
public static class BaseTest2 {
	const int MyConst = 5;
	static readonly BaseTest BaseTest = new BaseTest(MyConst);

    #region isochronous Tama main application
    [TamaTask(Task.IsochronousMain)]
    public static void IsochronousTamaMain() => BaseTest.IsochronousPart();
    #endregion isochronous Tama Main application
}
