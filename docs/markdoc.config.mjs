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
    section: {
      render: component("./src/components/LandingSection.astro"),
      attributes: {
        class: { type: String },
      },
    },
    "feature-card": {
      render: component("./src/components/FeatureCard.astro"),
      attributes: {
        icon: { type: String, required: true },
        title: { type: String, required: true },
      },
    },
    "tech-strip": {
      render: component("./src/components/TechStrip.astro"),
      selfClosing: true,
    },
    "value-flow": {
      render: component("./src/components/ValueFlow.astro"),
      selfClosing: true,
    },
    showcase: {
      render: component("./src/components/ShowcaseItem.astro"),
      attributes: {
        title: { type: String, required: true },
        image: { type: String },
        reverse: { type: Boolean },
      },
    },

  },
});
