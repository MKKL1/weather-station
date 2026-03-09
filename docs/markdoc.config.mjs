import { defineMarkdocConfig, component } from "@astrojs/markdoc/config";
import starlightMarkdoc from "@astrojs/starlight-markdoc";

export default defineMarkdocConfig({
  extends: [starlightMarkdoc()],
  nodes: {
    image: {
      render: component("./src/components/MarkdocImage.astro"),
      attributes: {
        src: { type: String, required: true },
        alt: { type: String },
        title: { type: String },
      },
    },
  },
  tags: {
    "themed-image": {
      render: component("./src/components/ThemedImage.astro"),
      attributes: {
        light: { type: String, required: true },
        dark: { type: String, required: true },
        alt: { type: String },
      },
      selfClosing: true,
    },
    ref: {
      render: component("./src/components/DocLinks.astro"),
      attributes: {
        terraform: { type: String },
        src: { type: String },
        azure: { type: String },
        repo: { type: String },
        inline: { type: Boolean },
      },
    },
    "api-btn": {
      render: component("./src/components/ApiButton.astro"),
      attributes: {
        method: { type: String, required: true },
        path: { type: String, required: true },
        spec: { type: String },
        tag: { type: String },
      },
      selfClosing: true,
    },
  },
});
