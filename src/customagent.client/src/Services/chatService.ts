import type { AuthorRole } from "../Slices/chatSlice";

export type ChatRequestModel={
    systemPrompt: string;
    messagePrompt: string;
    chatHistory: ChatMessage[];
};

export type ChatMessage = {
    role: AuthorRole;
    content: string;
}
export type Chunk = {
    Reference: string;
    Chunk: string;
    LastChunk: boolean;
    Error?: string;
}

export async function postChatMessage(model: ChatRequestModel, onChunk: (chunk: Chunk) => void) : Promise<void> {
    const response = await fetch("/api/chat/stream", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(model)
    });

    const reader = response.body!.getReader();
    const decoder = new TextDecoder();

    let buffer = '';

    while (true) {
        const { value, done } = await reader.read();

        if (done)
            break;

        buffer += decoder.decode(value, {
            stream: true
        });

        const events = buffer.split("\n\n");
        buffer = events.pop() ?? '';

        for (const event of events) {
            const data = event.startsWith("data: ") ? event.slice(6) : event;

            if (!data)
                continue;

            try {
                const chunk: Chunk = JSON.parse(data);
                onChunk(chunk);
            } catch (error) {
                let message = '';
                if (typeof error === "string") {
                    message = error.toUpperCase()
                } else if (error instanceof Error) {
                    message = error.message
                }
                console.log(`Error parsing: ${data}, message: ${message}`);
            }
        }
    }
}