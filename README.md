# What is AutoPidTuner?
  There's a major issue with FPV drones that are bigger than 5'' freestyle ones. They are big enough to have problems with managing themselves properly!

  Sometimes they have hard times finding how to match with their position with the level of RC input. Good news is that this problem can be solved via tuning PIDs!

  **AutoPidTuner** helps you to understand, why your drone feels weird to operate with and gives advice how to improve the overall situation. Also it provides CLI commands that you can paste into Betaflight Configurator to update certain PID values on certain axes.
# How to use
**Note! First of all finish setting up your filters and then it is reasonable to tune the PIDs.**
1. Make a test flight, make sure to have blackbox logging turned on and fly on all axes to have enough data
2. Open **[Betaflight Blackbox Explorer](https://blackbox.betaflight.com/)**
3. Open .BFL log file from blackbox there, click "Export CSV...":
![зображення](https://github.com/user-attachments/assets/f81caf37-230d-455d-aa0c-8ea5256e19af)
4. Open **AutoPidTuner**, open downloaded .csv file using this button:
![зображення](https://github.com/user-attachments/assets/9cbade23-460d-4e08-8834-8f753caa7851)
5. Wait some time. Processing takes small amount of time, but it depends
6. Below you'll see general info about aircraft, text written recommendations for correcting the PIDs and table with suggested new PID values
7. Copy CLI commands to change PID values such as they are displayed in table using this button:
![зображення](https://github.com/user-attachments/assets/c04fde8b-24c2-42a6-9be6-4f2c151535c0)
8. Open Betaflight Configurator, connect your drone, go to "CLI" tab and paste commands there. Don't forget to type _"save"_ and hit enter
9. Your quad will reconnect to Betaflight Configurator with updated PID values accordingly to **AutoPidTuner's** suggestions!
# What's next?
Don't forget that PID tuning is a looped process: _test flight -> tune -> test flight -> tune -> ..._

If you are satisfied with results, stop tuning and enjoy your quad's abilities to bring happiness to this world :) _(particularly a lot of happiness, if your FPV will attack russians)_
