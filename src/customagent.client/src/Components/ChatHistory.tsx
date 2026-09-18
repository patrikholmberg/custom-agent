import { selectChatHistory } from "../Slices/chatSlice";
import { useAppSelector } from "../Store/hooks";
import "./ChatHistory.css";

export default function ChatHistory() {
  const chatHistory = useAppSelector(selectChatHistory);
  return (
    <ul className="chat-history">
      {chatHistory.map((message) => {
        return (
          <li key={message.reference}>
            <div className="author">
              {message.role === "User" ? "You" : "Assistant"}
            </div>
            <div
              className="message"
              dangerouslySetInnerHTML={{ __html: message.formattedMessage }}
            ></div>
          </li>
        );
      })}
    </ul>
  );
}
