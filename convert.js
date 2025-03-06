const fs = require('fs');

const data = require('./seraph-faceless.json');
const animation_data = data["animations"];

flip_arms(animation_data, "BowAim");
flip_arms(animation_data, "BowAimCrude");
flip_arms(animation_data, "BowAimLong");
flip_arms(animation_data, "BowAimRecurve");

// const bowaim = animation_data.find(anim => anim.name === "BowAim");
// for(let i = 0; i < bowaim.keyframes.length; i++) {
//     let keyframe = bowaim.keyframes[i];
//     transform_frame(keyframe);
// }

function flip_arms(animation_data, anim_name) {
    let animation = animation_data.find(anim => anim.name === anim_name);
    animation["name"] = animation["name"] + "Fixed";
    animation["code"] = animation["code"] + "fixed";

    for(let i = 0; i < animation.keyframes.length; i++) {
        let keyframe = animation.keyframes[i];
        transform_frame(keyframe);
    }
}

// transformations required for bow animations
function transform_frame(frame) {
    let elements = frame.elements;
    swap_elements(elements, "UpperArmL", "UpperArmR");
    swap_elements(elements, "LowerArmL", "LowerArmR");
    swap_elements(elements, "UpperFootR", "UpperFootL");
    swap_elements(elements, "ItemAnchor", "ItemAnchorL");
    negate_elements(elements, ["ItemAnchor", "ItemAnchorL"], ["offsetZ", "rotationX", "rotationY"]);
    negate_elements(elements, ["UpperTorso", "LowerTorso", "Neck", "Head"], ["offsetZ", "rotationX", "rotationY"]);
    negate_elements(elements, ["UpperFootR", "UpperFootL"], ["rotationX", "rotationY"]);
    negate_elements(elements, ["UpperArmL", "LowerArmL", "UpperArmR", "LowerArmR"], ["offsetZ", "rotationX", "rotationY"]);
}

// Swaps the contents of two json elements
function swap_elements(elements, el_from, el_to) {
    const buffer = elements[el_from];
    elements[el_from] = elements[el_to];
    elements[el_to] = buffer;
}

// Negates a numbered json element (plz don't use with non-numbered)
function negate_elements(elements, els, values) {
    for(let i = 0; i < els.length; i++) {
        const element = elements[els[i]];
        if(element == null) continue;
        for(let j = 0; j < values.length; j++) {
            let value = element[values[j]];
            if(value != null) elements[els[i]][values[j]] = -value;
        }
    }
}

// Writes to file
const jsonString = JSON.stringify(data, null, 4);

fs.writeFile('new-seraph.json', jsonString, (err) => {
    if (err) {
        console.error("Error writing to file:", err);
    } else {
        console.log("JSON file has been saved.");
    }
});
