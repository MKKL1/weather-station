#include "Application.h"

void setup() {
    Application app;
    app.init();
    app.handleWakeup();
    app.goToSleep();
}

void loop() {}