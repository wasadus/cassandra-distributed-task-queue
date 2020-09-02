import * as React from "react";
import StoryRouter from "storybook-react-router";

import { TaskNotFoundPage } from "../../src/RemoteTaskQueueMonitoring/components/TaskNotFoundPage/TaskNotFoundPage";

export default {
    title: "RemoteTaskQueueMonitoring/TaskNotFoundPage",
    component: TaskNotFoundPage,
    decorators: [StoryRouter()],
};

export const Default = () => <TaskNotFoundPage parentLocation="http://google.com" />;
