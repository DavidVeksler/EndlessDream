# AI Image Generator

## Overview

This project is a web-based application designed to generate images using AI models. It utilizes Blazor for the front-end interface and connects to a backend service capable of processing image generation requests using Stable Diffusion models.

Demo: https://dream.davidveksler.com/image-generator

## Features

- **Prompt-based Image Generation**: Users can input text prompts to guide the AI in generating images.
- **Image Customization Options**: Includes settings for whether the image should be a photo, the number of steps for image processing, seed for randomness, and more.
- **Display of Generated Images**: Shows the generated images in the web interface, with options to view them in full-screen.

## Getting Started

### Prerequisites

- .NET 6.0 or later.
- A running instance of the image generation backend (details provided in the backend setup section).

### Setup

1. **Clone the repository**: `git clone [repository-url]`.
2. **Navigate to the project directory**: `cd [project-directory]`.
3. **Run the application**: Execute `dotnet run` in the command line within the project directory.

### Usage

1. **Access the Web Interface**: Open a web browser and navigate to `http://localhost:[port]/image-generator`.
2. **Enter Image Details**: Provide an image prompt, choose the desired settings, and click on `Render Image` or `Random Image`.
3. **View Generated Images**: The generated images will be displayed below the input form.

## Backend Setup

This application requires a Stable Diffusion backend service for processing image generation requests. Ensure that the backend is set up and accessible at the specified URL in the `ImageGenerator` class.

## Contributing

Contributions to enhance the application or fix issues are welcome. Please follow the standard procedures for contributing to a GitHub project.

## License

MIT License
