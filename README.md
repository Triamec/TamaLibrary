# TamaLibrary
Library examples of Tama programms

$Id: readme.txt 39154 2021-10-22 15:13:19Z ab $
Copyright © 2023 Triamec Motion AG

Caution: you may harm your hardware when executing sample applications 
without adjusting configuration values to your hardware environment.
Please read and follow the recommendations below
before executing any sample application.

Overview
--------

This "Tama Library" solution demonstrates how to write custom firmware extensions (Tama programs).
Some functionality may be used out of the box in custom programs, hence the term library.

All files containing Tama programs are given the ending .tama.cs in this sample.
After compiling, all Tama assemblies are in the bin\Release directory.

Note: Allow unsafe code is enabled in this project. However, you should not typically enable unsafe code unless you
      use the Reinterpret class. For more explanation, read the documentation of the mentioned class.


Hardware requirements
---------------------

- *Triamec* servo drive with logic supply, connected to your PC
- For the homing example: Drive power supply, motor, encoder and a stable position controller


What the programs do
---------------------

- Casting:
      Contains special converting routines.

- Homing:
      Sources of the Tama homing programs shipped with the TAM Software.

- StandaloneHomingAndEndswitches:
      Control drive via digital inputs. Includes homing and endswitch detection.

- PulseGenerator: 
      Generate periodic pulses on one or several digital outputs.

- PulseGeneratorTsd: 
      Generate periodic pulses at TSD Ax0..Ax1 DigOutputs 1..2.
      
- NoiseGenerator:
	  Generates a normal distributed random signal with a configurable RMS-value.

- BaseClassSample:
      Demonstrates how to write a set of almost identical Tama programs using a single source approach.

- Timers:
      Provides two timer implementations for different purposes.


Programming Primer
------------------

The task entry points are defined by the keyword "TamaTask" and are immediately followed by the desired function.
The "TamaTask" parameter specifies the type of task with the following properties:

[TamaTask(Task.AsynchronousMain)] : Task executed as fast as possible
                                    -> useful for complex state machines
                                    -> and time uncritical operations
[TamaTask(Task.IsochronousMain)]  : Task executed every 100us
                                    -> useful for hard realtime operations (e.g. kinematics)
                                    -> caution: very limited computing resources

The static class constructor - if defined - will be executed after downloading the program.

Static variables defined with "static ..." are persistent (e.g. static int counter = 0).
This means that the values will be stored when the task calculation has completed. They can be initialized before
entering the task for the first time.
The static variable initialization takes place when downloading the code. A start/stop of the task does not
initialize the variables.

 Important:
 Since every task has its own stack, it is not possible to exchange information from task to task by using static
 variables. Use the Tama general purpose registers instead.

 How to find a register?
 -> type an "R" and follow the Intellisense aid provided by Visual Studio.
 The registers are organized like in the TAM System Explorer register tree.


References
----------
- The Tama Compiler User Guide has details about the compilation.
- The Tama Compiler API and error documentation contains detailed information about each of the specific warning and error messages
  that you may encounter when compiling a Tama program. Note that the error code is only shown in the build output
  window.
- Enquire the TAM System Explorer Quick Start Guide on downloading and activating Tama programs.
- The TAM API Developer Manual introduces the APIs allowing to download and activate Tama assemblies programmatically.

Manuals: https://www.triamec.com/en/documents.html

- The Gear Up! sample application demonstrates another Tama program used for electronic gearing.

Gear Up! example: https://github.com/Triamec/GearUp

