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
        azure: { type: String },
        repo: { type: String },
        inline: { type: Boolean },
      },
    },
  },
});
