#include "Application.h"

void setup() {
    Application app;
    app.init();
    app.handleWakeup();
    Application::goToSleep();
}

void loop() {}