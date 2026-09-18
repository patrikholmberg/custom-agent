import { useEffect, useState } from "react";
import "./App.css";
import ChatComponent from "./Components/ChatComponent";

interface Forecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

function App() {
  return <ChatComponent />;
}

export default App;
